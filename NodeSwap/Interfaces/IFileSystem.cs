using System;

namespace NodeSwap.Interfaces;

public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void DeleteFile(string path);
    
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive = false);
    string[] GetDirectories(string path, string searchPattern = "*", System.IO.SearchOption searchOption = System.IO.SearchOption.TopDirectoryOnly);
    
    bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory);
}