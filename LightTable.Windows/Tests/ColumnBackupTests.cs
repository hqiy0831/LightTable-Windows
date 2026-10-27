using System.Security.Cryptography;
using System.Text.Json.Nodes;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class ColumnBackupTests : IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"LightTable-backup-tests-"+Guid.NewGuid().ToString("N"));
    string Dir(string name){var p=Path.Combine(root,name);Directory.CreateDirectory(p);return p;}
    BackupSourcePhoto Photo(string directory,string name,byte value=42,int bytes=1024)
    {var p=Path.Combine(Dir(directory),name);File.WriteAllBytes(p,Enumerable.Repeat(value,bytes).ToArray());return new(p,1.5);}
    static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    static void Mutate(string folder,Action<JsonObject> edit)
    {var path=Path.Combine(folder,ColumnBackup.ManifestName);var json=JsonNode.Parse(File.ReadAllText(path))!.AsObject();edit(json);File.WriteAllText(path,json.ToJsonString());}
    public void Dispose()
    {
        var absolute=Path.GetFullPath(root);var prefix=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!absolute.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(absolute).StartsWith("LightTable-backup-tests-",StringComparison.Ordinal))throw new IOException("Test cleanup escaped its own temporary root");
        if(Directory.Exists(absolute))Directory.Delete(absolute,true);
    }
    [Fact] public async Task WholeTrayCopiesExactOriginalBytesAndOrderWithoutChangingSources()
    {
        var a=Photo("source-a","同名照片.jpg",11);var b=Photo("source-b","同名照片.jpg",22);var originals=new[]{a,b,a};var before=originals.Select(p=>(Hash(p.Path),File.GetLastWriteTimeUtc(p.Path))).ToArray();
        var doc=new TableDocument{Photos=[new(){Path=a.Path},new(){Path=b.Path}]};doc.EnsureCollection();doc.Place(a.Path,new(1,1));
        var export=await ColumnBackup.ExportAsync(new("测试 栏目",originals),Dir("exports"));
        Assert.Equal(3,export.PhotoCount);Assert.Equal(3072,export.Bytes);Assert.DoesNotContain(root,File.ReadAllText(Path.Combine(export.Folder,ColumnBackup.ManifestName)));
        for(int i=0;i<originals.Length;i++){Assert.Equal(before[i].Item1,Hash(originals[i].Path));Assert.Equal(before[i].Item2,File.GetLastWriteTimeUtc(originals[i].Path));}
        var restored=await ColumnBackup.RestoreAsync(export.Folder,Dir("managed"));Assert.Equal("测试 栏目",restored.Name);Assert.Equal(3,restored.Photos.Count);
        Assert.Equal(new[]{before[0].Item1,before[1].Item1,before[2].Item1},restored.Photos.Select(p=>Hash(p.Path)));Assert.Equal(3,restored.Photos.Select(p=>p.Path).Distinct().Count());Assert.All(restored.Photos,p=>Assert.Equal("同名照片",p.Name));
        Assert.All(restored.Photos,p=>{Assert.Equal(Hash(p.Path),p.ContentHash);Assert.Equal(1024,p.SourceLength);Assert.Equal(File.GetLastWriteTimeUtc(p.Path).Ticks,p.SourceWriteTicks);});
    }
    [Fact] public async Task BackupCanBeRenamedMovedAndRestoredWithoutOriginalFiles()
    {
        var a=Photo("originals","one.jpg");var export=await ColumnBackup.ExportAsync(new("栏目一",[a]),Dir("cloud"));
        var renamed=Path.Combine(root,"downloaded-on-another-computer");Assert.StartsWith(root,Path.GetFullPath(renamed));Directory.Move(export.Folder,renamed);File.Delete(a.Path);
        var imported=await ColumnBackup.RestoreAsync(renamed,Dir("managed"));Assert.Single(imported.Photos);Assert.True(File.Exists(imported.Photos[0].Path));
        File.Delete(Path.Combine(renamed,"photos",Directory.GetFiles(Path.Combine(renamed,"photos")).Select(Path.GetFileName).Single()!));Assert.True(File.Exists(imported.Photos[0].Path));
    }
    [Fact] public async Task EmptyColumnIsPortableAndEveryExportGetsItsOwnFolder()
    {
        var parent=Dir("exports");var a=await ColumnBackup.ExportAsync(new("空栏目",[]),parent);var b=await ColumnBackup.ExportAsync(new("空栏目",[]),parent);Assert.NotEqual(a.Folder,b.Folder);
        var restored=await ColumnBackup.RestoreAsync(a.Folder,Dir("managed"));Assert.Empty(restored.Photos);Assert.Equal("空栏目",restored.Name);
    }
    [Fact] public async Task MissingSourceDoesNotLeaveACompletedOrPartialBackup()
    {
        var parent=Dir("exports");await Assert.ThrowsAsync<FileNotFoundException>(()=>ColumnBackup.ExportAsync(new("missing",[new(Path.Combine(root,"missing.jpg"),1)]),parent));Assert.Empty(Directory.GetFileSystemEntries(parent));
    }
    [Fact] public async Task CorruptSameLengthPhotoIsRejectedAndImportedStagingIsCleaned()
    {
        var export=await ColumnBackup.ExportAsync(new("tampered",[Photo("originals","a.jpg")]),Dir("exports"));var photo=Directory.GetFiles(Path.Combine(export.Folder,"photos")).Single();var bytes=File.ReadAllBytes(photo);bytes[0]^=255;File.WriteAllBytes(photo,bytes);
        var managed=Dir("managed");await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,managed));Assert.Empty(Directory.GetFileSystemEntries(managed));
    }
    [Fact] public async Task IncompleteDownloadIsRejectedBeforeCreatingAnyManagedFiles()
    {
        var export=await ColumnBackup.ExportAsync(new("missing",[Photo("originals","a.jpg")]),Dir("exports"));File.Delete(Directory.GetFiles(Path.Combine(export.Folder,"photos")).Single());var managed=Dir("managed");
        await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,managed));Assert.Empty(Directory.GetFileSystemEntries(managed));
    }
    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("photos/../outside.jpg")]
    [InlineData("C:/private.jpg")]
    [InlineData("photos/a.jpg:stream")]
    [InlineData("photos/..\\outside.jpg")]
    [InlineData("photos/CON.jpg")]
    public async Task UntrustedManifestCannotEscapeTheBackupFolder(string relative)
    {
        var export=await ColumnBackup.ExportAsync(new("unsafe",[Photo("originals","a.jpg")]),Dir("exports"));Mutate(export.Folder,j=>j["Photos"]![0]!["File"]=relative);
        await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,Dir("managed")));
    }
    [Fact] public async Task DuplicateEntriesAndUnsafeOriginalNamesAreRejected()
    {
        var export=await ColumnBackup.ExportAsync(new("unsafe",[Photo("originals","a.jpg")]),Dir("exports"));Mutate(export.Folder,j=>j["Photos"]!.AsArray().Add(j["Photos"]![0]!.DeepClone()));
        await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,Dir("managed")));
        Mutate(export.Folder,j=>{j["Photos"]!.AsArray().RemoveAt(1);j["Photos"]![0]!["OriginalName"]="../escape.jpg";});await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,Dir("managed")));
    }
    [Fact] public async Task UnsupportedVersionAndInvalidHashAreRejected()
    {
        var export=await ColumnBackup.ExportAsync(new("invalid",[Photo("originals","a.jpg")]),Dir("exports"));Mutate(export.Folder,j=>j["Version"]=99);await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,Dir("managed")));
        Mutate(export.Folder,j=>{j["Version"]=1;j["Photos"]![0]!["Sha256"]=new string('z',64);});await Assert.ThrowsAsync<InvalidDataException>(()=>ColumnBackup.RestoreAsync(export.Folder,Dir("managed")));
    }
    sealed class ImmediateProgress(Action<BackupProgress> action):IProgress<BackupProgress>{public void Report(BackupProgress p)=>action(p);}
    [Fact] public async Task CancelledExportLeavesSourcesAndExistingDestinationFilesUntouched()
    {
        var a=Photo("originals","large.jpg",55,1024*1024);var hash=Hash(a.Path);var parent=Dir("exports");var existing=Path.Combine(parent,"keep.txt");File.WriteAllText(existing,"untouched");using var cts=new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ColumnBackup.ExportAsync(new("cancel",[a]),parent,new ImmediateProgress(_=>cts.Cancel()),cts.Token));
        Assert.Equal(hash,Hash(a.Path));Assert.Equal(new[]{existing},Directory.GetFileSystemEntries(parent));Assert.Equal("untouched",File.ReadAllText(existing));
    }
    [Fact] public async Task CancelledRestoreDoesNotLeavePartialManagedCopies()
    {
        var export=await ColumnBackup.ExportAsync(new("cancel",[Photo("originals","large.jpg",55,1024*1024)]),Dir("exports"));using var cts=new CancellationTokenSource();var managed=Dir("managed");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ColumnBackup.RestoreAsync(export.Folder,managed,new ImmediateProgress(_=>cts.Cancel()),cts.Token));Assert.Empty(Directory.GetFileSystemEntries(managed));
    }
    [Fact] public async Task UncommittedImportCleanupIsConfinedToItsManagedGuidFolder()
    {
        var source=Photo("originals","a.jpg");var export=await ColumnBackup.ExportAsync(new("cleanup",[source]),Dir("exports"));var managed=Dir("managed");var imported=await ColumnBackup.RestoreAsync(export.Folder,managed);
        Assert.Throws<IOException>(()=>ColumnBackup.DiscardUncommittedRestore(new("unsafe",export.Folder,[]),managed));Assert.True(File.Exists(source.Path));Assert.True(Directory.Exists(export.Folder));
        ColumnBackup.DiscardUncommittedRestore(imported,managed);Assert.Empty(Directory.GetFileSystemEntries(managed));Assert.True(File.Exists(source.Path));Assert.True(Directory.Exists(export.Folder));
    }
}
