using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class EmailSaver
{
    private const string MATCH_EMAIL_PATTERN =
            @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
            + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
              + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
            + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

    private const string FILE_PATH = "Data/emails.txt";
    private static readonly HashSet<string> Emails = new HashSet<string>();

    public static bool SaveEmails = false;

    static EmailSaver()
    {
        if (!File.Exists(FILE_PATH))
            return;

        using (var sr = new StreamReader(FILE_PATH))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Emails.Add(line);
            }
        }
    }

    public static void SaveEmail(string email)
    {
        if (!SaveEmails)
            return;

        Emails.Add(email);
        File.WriteAllLines(FILE_PATH, Emails.ToArray());
    }

    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, MATCH_EMAIL_PATTERN);
    }
}