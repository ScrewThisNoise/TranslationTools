﻿using System;
using System.Collections.Generic;

namespace IllusionMods
{
    public delegate Dictionary<string, string> TranslationCollector();
    public delegate IEnumerable<TranslationDumper> TranslationGenerator();
    public class TranslationDumper
    {
        [Obsolete("Use Path")]
        public string Key => Path;

        [Obsolete("Use Collector")]
        public TranslationCollector Value => Collector;
        public string Path { get; }
        public TranslationCollector Collector { get; }

        public TranslationDumper(string path, TranslationCollector collector)
        {
            Path = path;
            Collector = collector;
        }
        public override string ToString()
        {
            return $"TranslationDumper<{Path}, {Collector}>";
        }
    }
}
