using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using RunningGearTracker_AquinoE.Model;

namespace RunningGearTracker_AquinoE.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

      

        [BindProperty(SupportsGet = true)]
        public int? EditGearId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteGearId { get; set; }

        // ── Display data 

        public List<Gear> Gears { get; set; } = new List<Gear>();
        public List<TrainingSessions> RecentSessions { get; set; } = new List<TrainingSessions>();
        public Gear CurrentGear { get; set; } = new Gear();
        public bool IsEditGear => EditGearId.HasValue;

        // ── OnGet 

        public void OnGet(string searchString)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Delete gear if DeleteGearId is in query string
            if (DeleteGearId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string deleteUsage = "DELETE FROM GearUsage WHERE GearID = @GearID";
                    using (SqlCommand cmd = new SqlCommand(deleteUsage, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", DeleteGearId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    string deleteGear = "DELETE FROM Gear WHERE GearID = @GearID";
                    using (SqlCommand cmd = new SqlCommand(deleteGear, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", DeleteGearId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                Response.Redirect("/");
                return;
            }

            // Load gear for editing if EditGearId is in query string
            if (EditGearId.HasValue)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Gear WHERE GearID = @GearID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", EditGearId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CurrentGear = new Gear
                                {
                                    GearID = Convert.ToInt32(reader["GearID"]),
                                    Brand = reader["Brand"].ToString(),
                                    ModelName = reader["ModelName"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    PurchaseDate = reader["PurchaseDate"] == DBNull.Value
                                                    ? ""
                                                    : Convert.ToDateTime(reader["PurchaseDate"]).ToString("yyyy-MM-dd")
                                };
                            }
                        }
                    }
                }
            }

            LoadData(searchString);
        }

        // ── OnPost — single method handles all form submissions
        //   "logSession" create a new training session + gear usage rows
        //   "addGear"     insert a new gear record
        //   "editGear"   update an existing gear record

        public IActionResult OnPost(
            string action,
            // Training session fields
            string ActivityDate,
            string Location,
            double? Distance_KM,
            string Duration,
            string AvgHeartRate,
            List<int> SelectedGearIds,
            // Gear fields (shared by addGear and editGear)
            int? GearID,
            string Brand,
            string ModelName,
            string Category,
            string PurchaseDate
        )
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // ── Log Session 
            if (action == "logSession")
            {
                double actualDistance = Distance_KM ?? 0.0;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string insertSession = "INSERT INTO TrainingSessions (ActivityDate, Location, Distance_KM, Duration, AvgHeartRate) " +
                                           "VALUES (@ActivityDate, @Location, @Distance_KM, @Duration, @AvgHeartRate); " +
                                           "SELECT SCOPE_IDENTITY();";

                    int newSessionId;
                    using (SqlCommand cmd = new SqlCommand(insertSession, conn))
                    {
                        cmd.Parameters.AddWithValue("@ActivityDate", string.IsNullOrEmpty(ActivityDate) ? (object)DBNull.Value : ActivityDate);
                        cmd.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(Location) ? (object)DBNull.Value : Location);
                        cmd.Parameters.AddWithValue("@Distance_KM", actualDistance);
                        cmd.Parameters.AddWithValue("@Duration", string.IsNullOrEmpty(Duration) ? (object)DBNull.Value : Duration);
                        cmd.Parameters.AddWithValue("@AvgHeartRate", string.IsNullOrEmpty(AvgHeartRate) ? (object)DBNull.Value : AvgHeartRate);

                        newSessionId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var gearId in SelectedGearIds)
                    {
                        string note = Request.Form[$"note_{gearId}"].ToString();

                        string insertUsage = "INSERT INTO GearUsage (GearID, SessionID, Notes) VALUES (@GearID, @SessionID, @Notes)";
                        using (SqlCommand cmd = new SqlCommand(insertUsage, conn))
                        {
                            cmd.Parameters.AddWithValue("@GearID", gearId);
                            cmd.Parameters.AddWithValue("@SessionID", newSessionId);
                            cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(note) ? (object)DBNull.Value : note);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // ── Add Gear ──────────────────────────────────────────────────────
            else if (action == "addGear")
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // GearID is NOT included — DB generates it via IDENTITY(1,1)
                    string insertQuery =
                        "INSERT INTO Gear (Brand, ModelName, Category, PurchaseDate) " +
                        "VALUES (@Brand, @ModelName, @Category, @PurchaseDate)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Brand",
                            string.IsNullOrEmpty(Brand) ? (object)DBNull.Value : Brand);
                        cmd.Parameters.AddWithValue("@ModelName",
                            string.IsNullOrEmpty(ModelName) ? (object)DBNull.Value : ModelName);
                        cmd.Parameters.AddWithValue("@Category",
                            string.IsNullOrEmpty(Category) ? (object)DBNull.Value : Category);
                        cmd.Parameters.AddWithValue("@PurchaseDate",
                            string.IsNullOrEmpty(PurchaseDate) ? (object)DBNull.Value : PurchaseDate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // ── Edit Gear
            else if (action == "editGear" && GearID.HasValue && GearID.Value > 0)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery =
                        "UPDATE Gear SET Brand = @Brand, ModelName = @ModelName, " +
                        "Category = @Category, PurchaseDate = @PurchaseDate " +
                        "WHERE GearID = @GearID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", GearID.Value);
                        cmd.Parameters.AddWithValue("@Brand",
                            string.IsNullOrEmpty(Brand) ? (object)DBNull.Value : Brand);
                        cmd.Parameters.AddWithValue("@ModelName",
                            string.IsNullOrEmpty(ModelName) ? (object)DBNull.Value : ModelName);
                        cmd.Parameters.AddWithValue("@Category",
                            string.IsNullOrEmpty(Category) ? (object)DBNull.Value : Category);
                        cmd.Parameters.AddWithValue("@PurchaseDate",
                            string.IsNullOrEmpty(PurchaseDate) ? (object)DBNull.Value : PurchaseDate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToPage("/Index");
        }



        private void LoadData(string searchString)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Load all gear (with optional Search)
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "";

                // Check if the user searched for something
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = "SELECT * FROM Gear WHERE Brand LIKE @search OR ModelName LIKE @search OR Category LIKE @search OR PurchaseDate LIKE @search";
                }
                else
                {
                    query = "SELECT * FROM Gear";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // If searching, add the parameter safely
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + searchString + "%");
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Gears.Add(new Gear
                            {
                                GearID = Convert.ToInt32(reader["GearID"]),
                                Brand = reader["Brand"].ToString(),
                                ModelName = reader["ModelName"].ToString(),
                                Category = reader["Category"].ToString(),
                                PurchaseDate = reader["PurchaseDate"] == DBNull.Value
                                                ? ""
                                                : Convert.ToDateTime(reader["PurchaseDate"]).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
            }

            // Load 3 most recent sessions
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TOP 3 * FROM TrainingSessions ORDER BY ActivityDate DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        RecentSessions.Add(new TrainingSessions
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
    }
}