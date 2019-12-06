using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NLock.Core
{
    public static class ZKPaths
    {
        public const string PATH_SEPARATOR = "/";

        private const char PATH_SEPARATOR_CHAR = '/';

        /// <summary>
        /// 给定完整路径，返回节点名。即“/one/two/three”将返回“three”
        /// </summary>
        /// <param name="path">完整路径</param>
        /// <returns></returns>
        public static string GetNodeFromPath(string path)
        {
            ValidatePath(path);

            int i = path.LastIndexOf(PATH_SEPARATOR_CHAR);
            if (i < 0)
            {
                return path;
            }
            if ((i + 1) >= path.Length)
            {
                return "";
            }
            return path.Substring(i + 1);
        }

        public static string MakePath(string parent, string child)
        {
            // 2 is the maximum number of additional path separators inserted
            int maxPathLength = NullableStringLength(parent) + NullableStringLength(child) + 2;
            // Avoid internal StringBuilder's buffer reallocation by specifying the max path length
            StringBuilder path = new StringBuilder(maxPathLength);

            JoinPath(path, parent, child);

            return path.ToString();
        }

        public static string MakePath(string parent, params string[] children)
        {
            // 2 is the maximum number of additional path separators inserted
            int maxPathLength = NullableStringLength(parent) + 2;
            if (children.Length > 0)
            {
                foreach (var child in children)
                {
                    // 1 is for possible additional separator
                    maxPathLength += NullableStringLength(child) + 1;
                }
            }
            // Avoid internal StringBuilder's buffer reallocation by specifying the max path length
            StringBuilder path = new StringBuilder(maxPathLength);

            if (children.Length == 0)
            {
                JoinPath(path, parent, "");
                return path.ToString();
            }
            else
            {
                foreach (var child in children)
                {
                    JoinPath(path, "", child);
                }
                return path.ToString();
            }
        }

        public static string ValidatePath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path), "Path cannot be null");
            }
            if (path.Length == 0)
            {
                throw new ArgumentNullException(nameof(path), "Path length must be > 0");
            }
            if (path[0] != '/')
            {
                throw new ArgumentException(nameof(path), "Path must start with / character");
            }
            if (path.Length == 1)
            { // done checking - it's the root
                return path;
            }
            if (path[path.Length - 1] == '/')
            {
                throw new ArgumentException(nameof(path), "Path must not end with / character");
            }

            string reason = null;
            var lastc = '/';
            var chars = path.ToCharArray();
            char c;
            for (int i = 1; i < chars.Length; lastc = chars[i], i++)
            {
                c = chars[i];

                if (c == 0)
                {
                    reason = "null character not allowed @" + i;
                    break;
                }
                else if (c == '/' && lastc == '/')
                {
                    reason = "empty node name specified @" + i;
                    break;
                }
                else if (c == '.' && lastc == '.')
                {
                    if (chars[i - 2] == '/' &&
                            ((i + 1 == chars.Length)
                                    || chars[i + 1] == '/'))
                    {
                        reason = "relative paths not allowed @" + i;
                        break;
                    }
                }
                else if (c == '.')
                {
                    if (chars[i - 1] == '/' &&
                            ((i + 1 == chars.Length)
                                    || chars[i + 1] == '/'))
                    {
                        reason = "relative paths not allowed @" + i;
                        break;
                    }
                }
                else if (c > '\u0000' && c < '\u001f'
                      || c > '\u007f' && c < '\u009F'
                      || c > '\ud800' && c < '\uf8ff'
                      || c > '\ufff0' && c < '\uffff')
                {
                    reason = "invalid charater @" + i;
                    break;
                }
            }

            if (reason != null)
            {
                throw new ArgumentException(nameof(path),
                        "Invalid path string \"" + path + "\" caused by " + reason);
            }

            return path;
        }

        private static int NullableStringLength(string s)
        {
            return s != null ? s.Length : 0;
        }

        private static void JoinPath(StringBuilder path, string parent, string child)
        {
            // 添加父项，不带尾随斜杠。
            if ((parent != null) && (parent.Length > 0))
            {
                if (parent[0] != PATH_SEPARATOR_CHAR)
                {
                    path.Append(PATH_SEPARATOR_CHAR);
                }
                if (parent[parent.Length - 1] == PATH_SEPARATOR_CHAR)
                {
                    path.Append(parent, 0, parent.Length - 1);
                }
                else
                {
                    path.Append(parent);
                }
            }

            if ((child == null) || (child.Length == 0) ||
                (child.Length == 1 && child[0] == PATH_SEPARATOR_CHAR))
            {
                // 特殊情况，父项子项都为空
                if (path.Length == 0)
                {
                    path.Append(PATH_SEPARATOR_CHAR);
                }
                return;
            }

            // 现在添加父级和子级之间的分隔符。
            path.Append(PATH_SEPARATOR_CHAR);

            int childAppendBeginIndex;
            if (child[0] == PATH_SEPARATOR_CHAR)
            {
                childAppendBeginIndex = 1;
            }
            else
            {
                childAppendBeginIndex = 0;
            }

            int childAppendEndIndex;
            if (child[child.Length - 1] == PATH_SEPARATOR_CHAR)
            {
                childAppendEndIndex = child.Length - 1;
            }
            else
            {
                childAppendEndIndex = child.Length;
            }

            // 最后，添加子项。
            path.Append(child, childAppendBeginIndex, childAppendEndIndex);
        }
    }
}
