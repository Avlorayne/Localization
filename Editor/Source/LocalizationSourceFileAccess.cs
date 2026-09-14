#if UNITY_EDITOR
using System.IO;

namespace Localization.Editor.Source
{
    internal static class LocalizationSourceFileAccess
    {
        public static FileStream OpenSharedRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        /// <summary>以共享读方式读取全部字节，避免与 Excel/正在写入的进程争抢文件锁。</summary>
        public static byte[] ReadAllBytesShared(string path)
        {
            using var stream = OpenSharedRead(path);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
#endif