using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using RunningGearTracker_AquinoE.Model;

namespace RunningGearTracker_AquinoE.Pages
{
    public class GearIndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public GearIndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Query string params (PDF pattern) ────────────────────────────────

        [BindProperty(SupportsGet = true)]
        public int? EditUsageId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteUsageId { get; set; }

        // ── Display data ──────────────────────────────────────────────────────

        public List<GearUsage> AllUsages { get; set; } = new List<GearUsage>();
        public List<TrainingSessions> AllSessions { get; set; } = new List<TrainingSessions>();
        public List<Gear> AllGears { get; set; } = new List<Gear>();
        public GearUsage CurrentUsage { get; set; } = new GearUsage();

        public bool IsEdit => EditUsageId.HasValue;

        // ── OnGet — handles delete + load-for-edit (PDF pattern) ─────────────

        public void OnGet()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Delete usage record
            if (DeleteUsageId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string deleteQuery = "DELETE FROM GearUsage WHERE UsageID = @UsageID";
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", DeleteUsageId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                Response.Redirect("/GearIndex");
                return;
            }

            // Load usage for editing
            if (EditUsageId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM GearUsage WHERE UsageID = @UsageID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", EditUsageId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CurrentUsage = new GearUsage
                                {
                                    UsageID = Convert.ToInt32(reader["UsageID"]),
                                    GearID = Convert.ToInt32(reader["GearID"]),
                                    SessionID = Convert.ToInt32(reader["SessionID"]),
                                    Notes = reader["Notes"] == DBNull.Value
                                                 ? ""
                                                 : reader["Notes"].ToString()
                                };
                            }
                        }
                    }
                }
            }

            LoadData();
        }

        // ── OnPost — single method, action hidden field branches logic ─────────
        // action values:
        //   "addUsage"  → insert new GearUsage row
        //   "editUsage" → update existing GearUsage row

        public IActionResult OnPost(
            string action,
            int? UsageID,
            int? GearID,
            int? SessionID,
            string Notes
        )
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // ── Add Usage ─────────────────────────────────────────────────────
            if (action == "addUsage")
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // UsageID not included — DB generates it via IDENTITY
                    string insertQuery =
                        "INSERT INTO GearUsage (GearID, SessionID, Notes) " +
                        "VALUES (@GearID, @SessionID, @Notes)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID",
                            GearID.HasValue ? (object)GearID.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SessionID",
                            SessionID.HasValue ? (object)SessionID.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(Notes) ? (object)DBNull.Value : Notes);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // ── Edit Usage ────────────────────────────────────────────────────
            else if (action == "editUsage" && UsageID.HasValue && UsageID.Value > 0)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery =
                        "UPDATE GearUsage " +
                        "SET GearID = @GearID, SessionID = @SessionID, Notes = @Notes " +
                        "WHERE UsageID = @UsageID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", UsageID.Value);
                        cmd.Parameters.AddWithValue("@GearID",
                            GearID.HasValue ? (object)GearID.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SessionID",
                            SessionID.HasValue ? (object)SessionID.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(Notes) ? (object)DBNull.Value : Notes);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToPage("/GearIndex");
        }

        // ── Load data helper ──────────────────────────────────────────────────

        private void LoadData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // All usages joined with Session and Gear
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query =
                    "SELECT " +
                    "    gu.UsageID, gu.Notes, " +
                    "    g.GearID, g.Brand, g.ModelName, g.Category, " +
                    "    s.SessionID, s.ActivityDate, s.Location, s.Distance_KM, s.Duration, s.AvgHeartRate " +
                    "FROM GearUsage gu " +
                    "INNER JOIN Gear g             ON gu.GearID    = g.GearID " +
                    "INNER JOIN TrainingSessions s ON gu.SessionID = s.SessionID " +
                    "ORDER BY s.ActivityDate DESC, g.Brand";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AllUsages.Add(new GearUsage
                        {
                            UsageID = Convert.ToInt32(reader["UsageID"]),
                            GearID = Convert.ToInt32(reader["GearID"]),
                            SessionID = Convert.ToInt32(reader["SessionID"]),
                            Notes = reader["Notes"] == DBNull.Value
                                         ? ""
                                         : reader["Notes"].ToString(),
                            Gear = new Gear
                            {
                                GearID = Convert.ToInt32(reader["GearID"]),
                                Brand = reader["Brand"].ToString(),
                                ModelName = reader["ModelName"].ToString(),
                                Category = reader["Category"].ToString()
                            },
                            TrainingSessions = new TrainingSessions
                            {
                                SessionID = Convert.ToInt32(reader["SessionID"]),
                                ActivityDate = reader["ActivityDate"] == DBNull.Value
                                                ? ""
                                                : Convert.ToDateTime(reader["ActivityDate"]).ToString("yyyy-MM-dd"),
                                Location = reader["Location"].ToString(),
                                Distance_KM = reader["Distance_KM"] == DBNull.Value
                                                ? 0
                                                : Convert.ToDouble(reader["Distance_KM"]),
                                Duration = reader["Duration"].ToString(),
                                AvgHeartRate = reader["AvgHeartRate"].ToString()
                            }
                        });
                    }
                }
            }

            // Sessions dropdown
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM TrainingSessions ORDER BY ActivityDate DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AllSessions.Add(new TrainingSessions
                        {
                            SessionID = Convert.ToInt32(reader["SessionID"]),
                            ActivityDate = reader["ActivityDate"] == DBNull.Value
                                            ? ""
                                            : Convert.ToDateTime(reader["ActivityDate"]).ToString("yyyy-MM-dd"),
                            Location = reader["Location"].ToString()
                        });
                    }
                }
            }

            // Gear dropdown
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Gear ORDER BY Brand";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AllGears.Add(new Gear
                        {
                            GearID = Convert.ToInt32(reader["GearID"]),
                            Brand = reader["Brand"].ToString(),
                            ModelName = reader["ModelName"].ToString(),
                            Category = reader["Category"].ToString()
                        });
                    }
                }
            }
        }
    }
}