using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MohaymenProject
{
    class Program
    {
        static string loggedInUser = null;

        static void Main(string[] args)
        {
            string connectionString = "Data Source=DESKTOP-HQB4U76;Initial Catalog=Mohaymen;Integrated Security=True";

            Console.WriteLine("Welcome! Available commands:");
            Console.WriteLine("1. Register --username [username] --password [password]");
            Console.WriteLine("2. Login --username [username] --password [password]");
            Console.WriteLine("3. Exit to close the program.");

            while (true)
            {
                if (loggedInUser == null)
                {
                    // Show Main Menu for non-logged-in users
                    Console.WriteLine("Available commands: Register, Login, Exit");
                }
                else
                {
                    // Show Menu for logged-in users
                    Console.WriteLine($"Welcome, {loggedInUser}. Available commands: Change, Search, ChangePassword, Logout");
                }

                Console.Write("Enter command: ");
                string input = Console.ReadLine();

                //Parse the input command into arguments
                string pattern = @"[\""].+?[\""]|[^ ]+";
                string[] commandArgs = Regex.Matches(input, pattern).Cast<Match>().Select(m => m.Value.Trim('"')).ToArray();

                if (commandArgs.Length == 0)
                {
                    Console.WriteLine("Invalid command.");
                    continue;
                }

                string command = commandArgs[0].ToLower();

                if (loggedInUser == null)
                {
                    // Commands for non-logged-in users
                    switch (command)
                    {
                        case "register":
                            HandleRegisterCommand(commandArgs, connectionString);
                            break;

                        case "login":
                            HandleLoginCommand(commandArgs, connectionString, ref loggedInUser);
                            break;

                        case "exit":
                            Console.WriteLine("Exiting application.");
                            return;

                        default:
                            Console.WriteLine("Unknown command. Please log in first.");
                            break;
                    }
                }
                else
                {
                    // Commands for logged-in users
                    switch (command)
                    {
                        case "change":
                            HandleChangeCommand(commandArgs, connectionString, loggedInUser);
                            break;

                        case "search":
                            HandleSearchCommand(commandArgs, connectionString);
                            break;
                        case "changepassword":
                            HandleChangePasswordCommand(commandArgs, connectionString, ref loggedInUser);
                            break;
                        case "logout":
                            loggedInUser = null;
                            Console.WriteLine("Logged out successfully.");
                            break;

                        default:
                            Console.WriteLine("Unknown command. Please use a valid command.");
                            break;
                    }
                }
            }
        }
        static void HandleRegisterCommand(string[] args, string connectionString)
        {
            if (args.Length < 5 || args[1].ToLower() != "--username" || args[3].ToLower() != "--password")
            {
                Console.WriteLine("Invalid command. Use: Register --username [username] --password [password]");
                return;
            }

            string username = args[2];
            string password = args[4];
            string hashedPassword = HashPassword(password);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the username already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE username = @username";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@username", username);
                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        Console.WriteLine("Register failed! Username already exists.");
                        return;
                    }
                }

                // Insert new user
                string insertQuery = "INSERT INTO Users (username, password, status) VALUES (@username, @password, @status)";
                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@username", username);
                    insertCommand.Parameters.AddWithValue("@password", hashedPassword);
                    insertCommand.Parameters.AddWithValue("@status", "Available"); // Default status

                    int rowsAffected = insertCommand.ExecuteNonQuery();
                    Console.WriteLine(rowsAffected > 0 ? "Register Complete" : "Register failed!");
                }
            }
        }
        static void HandleLoginCommand(string[] args, string connectionString, ref string loggedInUser)
        {
            if (args.Length < 5 || args[1].ToLower() != "--username" || args[3].ToLower() != "--password")
            {
                Console.WriteLine("Invalid command. Use: Login --username [username] --password [password]");
                return;
            }

            string username = args[2];
            string password = args[4];
            string hashedPassword = HashPassword(password);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Users WHERE username = @username AND password = @password";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", hashedPassword);

                    int count = (int)command.ExecuteScalar();
                    if (count > 0)
                    {
                        loggedInUser = username;
                        Console.WriteLine("Login successful.");
                    }
                    else
                    {
                        Console.WriteLine("Login failed! Invalid username or password.");
                    }
                }
            }
        }
        static void HandleChangeCommand(string[] args, string connectionString, string loggedInUser)
        {
            if (args.Length < 3 || args[1].ToLower() != "--status")
            {
                Console.WriteLine("Invalid command. Use: Change --status [available/not available]");
                return;
            }

            string newStatus = args[2].ToLower();
            if (newStatus != "available" && newStatus != "not available")
            {
                Console.WriteLine("Invalid status. Use 'available' or 'not available'.");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "UPDATE Users SET status = @status WHERE username = @username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", newStatus);
                        command.Parameters.AddWithValue("@username", loggedInUser);

                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine(rowsAffected > 0 ? "Status updated successfully." : "Failed to update status.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }
        static void HandleSearchCommand(string[] args, string connectionString)
        {
            if (args.Length < 3 || args[1].ToLower() != "--username")
            {
                Console.WriteLine("Invalid command. Use: Search --username [username]");
                return;
            }

            string searchUsername = args[2];

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT username, status FROM Users WHERE username LIKE @search";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@search", $"%{searchUsername}%");
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("No users found.");
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                string username = reader.GetString(0);
                                string status = reader.GetString(1);
                                Console.WriteLine($"{username} | {status}");
                            }
                        }
                    }
                }
            }
        }
        static void HandleChangePasswordCommand(string[] args, string connectionString, ref string loggedInUser)
        {
            if (args.Length < 5 || args[1].ToLower() != "--old" || args[3].ToLower() != "--new")
            {
                Console.WriteLine("Invalid command. Use: ChangePassword --old [oldPassword] --new [newPassword]");
                return;
            }

            string oldPassword = args[2];
            string newPassword = args[4];

            string oldPasswordHash = HashPassword(oldPassword); // Hash the old password
            string newPasswordHash = HashPassword(newPassword); // Hash the new password

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the old password matches
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE username = @username AND password = @password";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@username", loggedInUser);
                    checkCommand.Parameters.AddWithValue("@password", oldPasswordHash);

                    int count = (int)checkCommand.ExecuteScalar();
                    if (count == 0)
                    {
                        Console.WriteLine("ChangePassword failed! Old password is incorrect.");
                        return;
                    }
                }

                // Update the password
                string updateQuery = "UPDATE Users SET password = @newPassword WHERE username = @username";
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@username", loggedInUser);
                    updateCommand.Parameters.AddWithValue("@newPassword", newPasswordHash);

                    int rowsAffected = updateCommand.ExecuteNonQuery();
                    if(rowsAffected > 0)
                    {
                        Console.WriteLine("Password changed successfully. Please Login Again!");
                        loggedInUser = null;
                    }
                    else
                    {
                        Console.WriteLine("Failed to change password.");
                    }
                }
            }
        }
        static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Convert to hexadecimal
                }
                return builder.ToString();
            }
        }


    }
}
