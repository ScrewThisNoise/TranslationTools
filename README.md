# TranslationTools
Translation tools for Illusion games

## Plugins

### TextResourceRedirector

- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r12/AI_TextResourceRedirector.v1.4.2.zip)** - For AI Girl
- **v1.1.1 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r2/HS_TextResourceRedirector.v1.1.1.zip)** - For Honey Select
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r12/HS2_TextResourceRedirector.v1.4.2.zip)** - For Honey Select 2
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r12/KK_TextResourceRedirector.v1.4.2.zip)** - For Koikatsu



Allows translations to override individual assets. Required for some translations to function correctly. Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) v4.11 or greater.

### TextDump

- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r11/AI_TextDump.v1.4.2.zip)** - For AI Girl
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r11/AI_INT_TextDump.v1.4.2.zip)** - For AI Shoujo (Steam)
- **v1.1 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r2/HS_TextDump.v1.1.zip)** - For Honey Select
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r11/HS2_TextDump.v1.4.2.zip)** - For Honey Select 2
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r11/KK_TextDump.v1.4.2.zip)** - For Koikatsu
- **v1.4.2 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r11/KKP_TextDump.v1.4.2.zip)** - For Koikatsu Party


Dumps untranslated text in to .txt files so that the lines can be used by translators working on the translation projects. Normally only executes under studio on initial load. Versions for localized games run under the main game and require multiple dump stages. Check the console and/or log file for specifics.

Marked incompatible with [XUnity.AutoTranslator and XUnity.ResourceRedirector](https://github.com/bbepis/XUnity.AutoTranslator), since if either of them are installed it may not dump the unmodified assets.

### TranslationSync

- **v1.3.1 - [Download](https://github.com/IllusionMods/TranslationTools/releases/download/r4/KK_TranslationSync.v1.3.1.zip)** - For Koikatsu

A plugin for correctly formatting translation files. Corrects formatting and copies translations from one file to another for the same personality in case of duplicate entries. Used by translators working on the [Koikatsu Story Translation](https://github.com/IllusionMods/KoikatsuStoryTranslation) project. No need to download unless you're working on translations.

To use, open the plugin settings and set a personality, press the hotkey (default 0) to sync translations. Read your bepinex console or output_log.txt to see the changes made or any warnings and errors. Press alt+hotkey to force sync translation files in case of differing translations (warning: make backups first. It may not be obvious which translations are treated as the primary source). Press ctrl+hotkey to sync translations for all personalities (warning: very slow).

Only works on extracted files (not zip archives), and uses [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) configuration to determine where files should be located.

## Tools

### MergeIntoDump

Command line utility to any lines found in a dump to a set of asset translation files.  Works only on extracted files, modifies output files in place (**back up your files before using**).

```
> MergeInfoDump.exe TextDump\RedirectedResources BepInEx\Translations\en\RedirectedResources
```



