namespace ProjectsGenerator;

public static class ChecksConstants
{
    public const string Contacts = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static readonly string[] CheckTypes =
    [
        "напряжения",
        "сопротивления",
        "тока",
    ];

    public static readonly string[] Objects =
    [
        "ЛОГ",
        "МОД",
        "ПРОД",
        "МИД",
        "МУР",
        "ВРМ",
        "БРМ"
    ];

    public static readonly Dictionary<string, string[]> Modifications = new()
    {
        { "ЛОГ", ["ЛОГ0", "ЛОГ4", "ЛОГ5", "ЛОГ7", "ЛОГ9"] },
        { "МОД", ["МОД1", "МОД2", "МОД4", "МОД6", "МОД7"] },
        { "ПРОД", ["ПРОД2", "ПРОД3", "ПРОД6", "ПРОД8", "ПРОД11"] },
        { "МИД", ["МИД1", "МИД2", "МИД3", "МИД6", "МИД9"] },
        { "МУР", ["МУР1", "МУР3", "МУР8", "МУР9", "МУР11", "МУР12"] },
        { "ВРМ", ["ВРМ1", "ВРМ3", "ВРМ6", "ВРМ9"] },
        { "БРМ", ["БРМ0", "БРМ1", "БРМ3", "БРМ5", "БРМ7", "БРМ9", "БРМ11"] }
    };

    public static T RandomElement<T>(T[] arr)
    {
        return arr[Random.Shared.Next(0, arr.Length)];
    }
}