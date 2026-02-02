Enums for ISO languages, countries and currencies

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/IsoEnums)](https://www.nuget.org/packages/IsoEnums/)
![GitHub License](https://img.shields.io/github/license/darcara/IsoEnums)

# Common

All enums have the value `0` as `Uninitialized`

Values that have been removed recently, will still be available in the enums, but are annotated with the `[Obsolete]` Attribute 

# ISO639 Language

The `Language` enum contains 7319 languages.

```csharp
// Every language is available in the Language-enum
Language l = Language.English;
// Every Language has a 639-3 3-letter-code
String iso639_3Code = Language.English.Get3Code();  // eng
// A few languages have 2-letter-code
String iso639_2Code = Language.English.Get2Code();  // en
String nonexistant_2Code = Language.Dari.Get2Code();  // ??

// Lookup any language by their code (Char or Byte)
Language english = LanguageHelper.GetLanguageByCode("en");
Language english = LanguageHelper.GetLanguageByCode("en-us");
Language english = LanguageHelper.GetLanguageByCode("eng");

// There are also specific lookups if you know what type you want to look up
Language english = LanguageHelper.GetLanguageBy2Code("en");
Language english = LanguageHelper.GetLanguageBy2Code("en-us");
Language english = LanguageHelper.GetLanguageBy3Code("eng");

// For bulk lookups use the Dictionaries from LanguageHelper
FrozenDictionary<String, Language> lookup = LanguageHelper.CreateFast2CodeLookup();
Language english = lookup["en"];

// The dictionary lookup will not work for language-country lookups
Language english = lookup["en-us"];
```

There are a few ``SpecialLanguageCodes`` as defined by the standard
* ``Language.Uninitialized`` - The zero (0) enum value
* ``Language.Uncoded_languages`` - not used
* ``Language.Multiple_languages``  - not used
* ``Language.Undetermined`` - returned by `GetLanguageByCode` for unknown language codes 
* ``Language.No_linguistic_content`` - not used

# ISO3166 Countries

The `Country` enum contains 252 countries.

# ISO4217 Currency

The `Currency` enum contains 171 currencies.