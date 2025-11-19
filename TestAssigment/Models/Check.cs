namespace TestAssigment.Models;

public class Check(
    ulong n,
    string contact1,
    string contact2,
    string checkType,
    string o,
    string modifications,
    int? checkResult)
{
    public ulong N { get; set; } = n;
    public string Contact1 { get; set; } = contact1;
    public string Contact2 { get; set; } = contact2;
    public string CheckType { get; set; } = checkType;
    public string Object { get; set; } = o;
    public string Modifications { get; set; } = modifications;
    public int? CheckResult { get; set; } = checkResult;
}