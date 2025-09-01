using System.Globalization;
using UnityEngine;

public class Datahandling : MonoBehaviour
{
    public StewartIK_DeltaAB smallStewart;

    public void inputSorter(string input)
    {
        // also cut at newline in case multiple packets concatenated
        int nl = input.IndexOf('\n');
        if (nl >= 0) input = input.Substring(0, nl);

        string[] p = input.Split(new[] {','}, System.StringSplitOptions.RemoveEmptyEntries);
        if (p.Length >= 7) stewartData(p);
    }

    static string Clean(string s)
        => s.Trim().Trim('\0', '\r', '\n', ' '); // remove NULs & whitespace

    void stewartData(string[] p)
    {
        var inv = CultureInfo.InvariantCulture;

        float surge = float.Parse(Clean(p[0]), inv);
        float sway  = float.Parse(Clean(p[1]), inv);
        float heave = float.Parse(Clean(p[2]), inv);
        float roll  = float.Parse(Clean(p[3]), inv);
        float pitch = float.Parse(Clean(p[4]), inv);
        float yaw   = float.Parse(Clean(p[5]), inv);

        string idTok = Clean(p[6]);
        // prefer integer compare for identifiers
        if (int.TryParse(idTok, NumberStyles.Integer, inv, out int id) && id == 1)
        {
            smallStewart.realTimeInput(surge, sway, heave, roll, pitch, yaw);
        }
        else
        {
            // Big stewart (id != 1)
            Debug.Log($"Other ID: {idTok}");
        }
    }
}
