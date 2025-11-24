namespace TestAssigment.Models;

public class Check(
    ulong n,
    string contact1,
    string port1,
    string contact2,
    string port2,
    string checkType,
    string o,
    string modifications,
    int? checkResult)
{
    public ulong N { get; set; } = n;
    public string Contact1 { get; set; } = contact1;
    public string Port1 { get; set; } = port1;
    public string Contact2 { get; set; } = contact2;
    public string Port2 { get; set; } = port1;
    public string CheckType { get; set; } = checkType;
    public string Object { get; set; } = o;
    public string Modifications { get; set; } = modifications;
    public int? CheckResult { get; set; } = checkResult;
}