using System;

namespace PackageDependencyAnalysis.ContextObjects
{
    [Flags]
    public enum ProjectItemType
    {
        /// <summary>
        /// "None" type
        /// </summary>
        None = 1,

        /// <summary>
        /// "Compile" type
        /// </summary>
        Compile = 2,

        /// <summary>
        /// All supported types
        /// </summary>
        All = 3
    }
}