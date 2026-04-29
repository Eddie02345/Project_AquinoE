using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using RunningGearTracker_AquinoE.Model;

namespace RunningGearTracker_AquinoE.Pages.Sessions
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Query string params

        [BindProperty(SupportsGet = true)]
        public int? EditSessionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteSessionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ViewGearSessionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EditUsageId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteUsageId { get; set; }

        // ── Display data

        public List<TrainingSessions> AllSessions { get; set; } = new List<TrainingSessions>();
        public List<Gear> AllGears { get; set; } = new List<Gear>();
        public List<GearUsage> SessionGearUsages { get; set; } = new List<GearUsage>();

        public TrainingSessions CurrentSession { get; set; } = new TrainingSessions();
        public GearUsage CurrentUsage { get; set; } = new GearUsage();

        public bool IsEditSession => EditSessionId.HasValue;
        public bool IsEditUsage => EditUsageId.HasValue;

        // ── OnGet — handles all deletes + load-for-edit 

        public void OnGet(string searchString)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Delete session
            if (DeleteSessionId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string deleteUsages = "DELETE FROM GearUsage WHERE SessionID = @SessionID";
                    using (SqlCommand cmd = new SqlCommand(deleteUsages, conn))
                    {
                        cmd.Parameters.AddWithValue("@SessionID", DeleteSessionId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    string deleteSession = "DELETE FROM TrainingSessions WHERE SessionID = @SessionID";
                    using (SqlCommand cmd = new SqlCommand(deleteSession, conn))
                    {
                        cmd.Parameters.AddWithValue("@SessionID", DeleteSessionId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                Response.Redirect("/Sessions");
                return;
            }

            // Delete gear usage row
            if (DeleteUsageId.HasValue)
            {
                int returnSessionId = 0;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Grab the SessionID before deleting so we can return to the gear panel
                    string getSession = "SELECT SessionID FROM GearUsage WHERE UsageID = @UsageID";
                    using (SqlCommand cmd = new SqlCommand(getSession, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", DeleteUsageId.Value);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                            returnSessionId = Convert.ToInt32(result);
                    }

                    string deleteUsage = "DELETE FROM GearUsage WHERE UsageID = @UsageID";
                    using (SqlCommand cmd = new SqlCommand(deleteUsage, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", DeleteUsageId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                Response.Redirect($"/Sessions?viewGearSessionId={returnSessionId}");
                return;
            }

            // Load session for editing
            if (EditSessionId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM TrainingSessions WHERE SessionID = @SessionID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SessionID", EditSessionId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CurrentSession = new TrainingSessions
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
                                };
                            }
                        }
                    }
                }
            }

            // Load gear usage row for editing
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
                                    Notes = reader["Notes"].ToString()
                                };
                            }
                        }
                    }
                }
            }

            // Load gear usages for the open session panel
            if (ViewGearSessionId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query =
                        "SELECT gu.UsageID, gu.GearID, gu.SessionID, gu.Notes, " +
                        "g.Brand, g.ModelName, g.Category " +
                        "FROM GearUsage gu " +
                        "INNER JOIN Gear g ON gu.GearID = g.GearID " +
                        "WHERE gu.SessionID = @SessionID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SessionID", ViewGearSessionId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SessionGearUsages.Add(new GearUsage
                                {
                                    UsageID = Convert.ToInt32(reader["UsageID"]),
                                    GearID = Convert.ToInt32(reader["GearID"]),
                                    SessionID = Convert.ToInt32(reader["SessionID"]),
                                    Notes = reader["Notes"].ToString(),
                                    Gear = new Gear
                                    {
                                        GearID = Convert.ToInt32(reader["GearID"]),
                                        Brand = reader["Brand"].ToString(),
                                        ModelName = reader["ModelName"].ToString(),
                                        Category = reader["Category"].ToString()
                                    }
                                });
                            }
                        }
                    }
                }
            }

            LoadData(searchString);
        }

        // ── OnPost — single method, action hidden field branches the logic ─────
        // action values:
        //   "addSession"    insert new training session
        //   "editSession"  update existing training session
        //   "addUsage"     insert new gear usage row
        //   "editUsage"     update existing gear usage row

        public IActionResult OnPost(
            string action,
            // Session fields
            int? SessionID,
            string ActivityDate,
            string Location,
            double? Distance_KM,
            string Duration,
            string AvgHeartRate,
            // Gear usage fields
            int? UsageID,
            int? GearID,
            int? UsageSessionID,
            string Notes
        )
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // ── Add Session 
            if (action == "addSession")
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                   
                    string insertQuery =
                        "INSERT INTO TrainingSessions (ActivityDate, Location, Distance_KM, Duration, AvgHeartRate) " +
                        "VALUES (@ActivityDate, @Location, @Distance_KM, @Duration, @AvgHeartRate)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ActivityDate",
                            string.IsNullOrEmpty(ActivityDate) ? (object)DBNull.Value : ActivityDate);
                        cmd.Parameters.AddWithValue("@Location",
                            string.IsNullOrEmpty(Location) ? (object)DBNull.Value : Location);
                        cmd.Parameters.AddWithValue("@Distance_KM",
                            Distance_KM.HasValue ? (object)Distance_KM.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Duration",
                            string.IsNullOrEmpty(Duration) ? (object)DBNull.Value : Duration);
                        cmd.Parameters.AddWithValue("@AvgHeartRate",
                            string.IsNullOrEmpty(AvgHeartRate) ? (object)DBNull.Value : AvgHeartRate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // ── Edit Session
            else if (action == "editSession" && SessionID.HasValue && SessionID.Value > 0)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery =
                        "UPDATE TrainingSessions " +
                        "SET ActivityDate = @ActivityDate, Location = @Location, " +
                        "Distance_KM = @Distance_KM, Duration = @Duration, AvgHeartRate = @AvgHeartRate " +
                        "WHERE SessionID = @SessionID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@SessionID", SessionID.Value);
                        cmd.Parameters.AddWithValue("@ActivityDate",
                            string.IsNullOrEmpty(ActivityDate) ? (object)DBNull.Value : ActivityDate);
                        cmd.Parameters.AddWithValue("@Location",
                            string.IsNullOrEmpty(Location) ? (object)DBNull.Value : Location);
                        cmd.Parameters.AddWithValue("@Distance_KM",
                            Distance_KM.HasValue ? (object)Distance_KM.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Duration",
                            string.IsNullOrEmpty(Duration) ? (object)DBNull.Value : Duration);
                        cmd.Parameters.AddWithValue("@AvgHeartRate",
                            string.IsNullOrEmpty(AvgHeartRate) ? (object)DBNull.Value : AvgHeartRate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // ── Add Gear Usage 
            else if (action == "addUsage" && GearID.HasValue && UsageSessionID.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

     
                    string insertQuery =
                        "INSERT INTO GearUsage (GearID, SessionID, Notes) " +
                        "VALUES (@GearID, @SessionID, @Notes)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", GearID.Value);
                        cmd.Parameters.AddWithValue("@SessionID", UsageSessionID.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(Notes) ? (object)DBNull.Value : Notes);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Redirect($"/Sessions?viewGearSessionId={UsageSessionID.Value}");
            }

            // ── Edit Gear Usage 
            else if (action == "editUsage" && UsageID.HasValue && UsageID.Value > 0)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery =
                        "UPDATE GearUsage SET GearID = @GearID, Notes = @Notes " +
                        "WHERE UsageID = @UsageID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsageID", UsageID.Value);
                        cmd.Parameters.AddWithValue("@GearID",
                            GearID.HasValue ? (object)GearID.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(Notes) ? (object)DBNull.Value : Notes);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Redirect($"/Sessions?viewGearSessionId={UsageSessionID}");
            }

            return RedirectToPage("/Sessions");
        }


        private void LoadData(string searchString)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // All sessions ordered newest first (with Search applied)
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "";

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = "SELECT * FROM TrainingSessions WHERE Location LIKE @search ORDER BY ActivityDate DESC";
                }
                else
                {
                    query = "SELECT * FROM TrainingSessions ORDER BY ActivityDate DESC";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + searchString + "%");
                    }

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
                                Location = reader["Location"].ToString(),
                                Distance_KM = reader["Distance_KM"] == DBNull.Value
                                                ? 0
                                                : Convert.ToDouble(reader["Distance_KM"]),
                                Duration = reader["Duration"].ToString(),
                                AvgHeartRate = reader["AvgHeartRate"].ToString()
                            });
                        }
                    }
                }
            }

            // All gear for dropdowns
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Gear";
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