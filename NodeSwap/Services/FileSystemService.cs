using System;
using System.IO;
using System.Runtime.InteropServices;
using NodeSwap.Interfaces;

namespace NodeSwap.Services;

public partial class FileSystemService : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    
    public string ReadAllText(string path) => File.ReadAllText(path);
    
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    
    public void DeleteFile(string path) => File.Delete(path);
    
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    
    public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);
    
    public string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetDirectories(path, searchPattern, searchOption);
    
    public bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        var result = CreateSymbolicLinkWin32(linkPath, targetPath, isDirectory ? SymbolicLink.Directory : SymbolicLink.File);
        return result;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateSymbolicLink", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateSymbolicLinkWin32(
        string lpSymlinkFileName,
        string lpTargetFileName,
        SymbolicLink dwFlags);

    private enum SymbolicLink
    {
        File = 0,
        Directory = 1,
    }
}