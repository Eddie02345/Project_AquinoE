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

        // ── Bound Properties ─────────────────────────────────────────────────

        [BindProperty]
        public TrainingSessions NewSession { get; set; } = new TrainingSessions();

        // Which gear checkboxes are ticked
        [BindProperty]
        public List<int> SelectedGearIds { get; set; } = new List<int>();

        // Notes keyed by GearID — e.g. GearNotes[3] = "Felt tight on left foot"
        [BindProperty]
        public Dictionary<int, string> GearNotes { get; set; } = new Dictionary<int, string>();

        [BindProperty]
        public Gear NewGear { get; set; } = new Gear();

        [BindProperty]
        public Gear EditGear { get; set; } = new Gear();

        // Edit / Delete via query string (PDF pattern)
        [BindProperty(SupportsGet = true)]
        public int? EditGearId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteGearId { get; set; }

        // ── Display Data ──────────────────────────────────────────────────────

        public List<Gear> Gears { get; set; } = new List<Gear>();
        public List<TrainingSessions> RecentSessions { get; set; } = new List<TrainingSessions>();
        public Gear CurrentGear { get; set; } = new Gear();
        public bool IsEditGear => EditGearId.HasValue;

        // ── OnGet ─────────────────────────────────────────────────────────────

        public void OnGet()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Delete gear if DeleteGearId is provided (PDF pattern: handle in OnGet)
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

            // Load gear for editing if EditGearId is provided (PDF pattern: load in OnGet)
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

            LoadData();
        }

        // ── Create Training Session ───────────────────────────────────────────

        public IActionResult OnPostCreateSession()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Insert session — DB auto-generates SessionID via IDENTITY
                string insertSession =
                    "INSERT INTO TrainingSessions (ActivityDate, Location, Distance_KM, Duration, AvgHeartRate) " +
                    "VALUES (@ActivityDate, @Location, @Distance_KM, @Duration, @AvgHeartRate); " +
                    "SELECT SCOPE_IDENTITY();";

                int newSessionId;
                using (SqlCommand cmd = new SqlCommand(insertSession, conn))
                {
                    cmd.Parameters.AddWithValue("@ActivityDate",
                        string.IsNullOrEmpty(NewSession.ActivityDate) ? (object)DBNull.Value : NewSession.ActivityDate);
                    cmd.Parameters.AddWithValue("@Location",
                        string.IsNullOrEmpty(NewSession.Location) ? (object)DBNull.Value : NewSession.Location);
                    cmd.Parameters.AddWithValue("@Distance_KM", NewSession.Distance_KM);
                    cmd.Parameters.AddWithValue("@Duration",
                        string.IsNullOrEmpty(NewSession.Duration) ? (object)DBNull.Value : NewSession.Duration);
                    cmd.Parameters.AddWithValue("@AvgHeartRate",
                        string.IsNullOrEmpty(NewSession.AvgHeartRate) ? (object)DBNull.Value : NewSession.AvgHeartRate);

                    newSessionId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Insert one GearUsage row per selected gear, each with its own note
                foreach (var gearId in SelectedGearIds)
                {
                    GearNotes.TryGetValue(gearId, out string note);

                    string insertUsage =
                        "INSERT INTO GearUsage (GearID, SessionID, Notes) VALUES (@GearID, @SessionID, @Notes)";
                    using (SqlCommand cmd = new SqlCommand(insertUsage, conn))
                    {
                        cmd.Parameters.AddWithValue("@GearID", gearId);
                        cmd.Parameters.AddWithValue("@SessionID", newSessionId);
                        cmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrEmpty(note) ? (object)DBNull.Value : note);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToPage("/Index");
        }

        // ── Create Gear ───────────────────────────────────────────────────────
        // GearID is NOT included — the DB generates it automatically via IDENTITY(1,1)

        public IActionResult OnPostCreateGear()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string insertQuery =
                    "INSERT INTO Gear (Brand, ModelName, Category, PurchaseDate) " +
                    "VALUES (@Brand, @ModelName, @Category, @PurchaseDate)";

                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Brand",
                        string.IsNullOrEmpty(NewGear.Brand) ? (object)DBNull.Value : NewGear.Brand);
                    cmd.Parameters.AddWithValue("@ModelName",
                        string.IsNullOrEmpty(NewGear.ModelName) ? (object)DBNull.Value : NewGear.ModelName);
                    cmd.Parameters.AddWithValue("@Category",
                        string.IsNullOrEmpty(NewGear.Category) ? (object)DBNull.Value : NewGear.Category);
                    cmd.Parameters.AddWithValue("@PurchaseDate",
                        string.IsNullOrEmpty(NewGear.PurchaseDate) ? (object)DBNull.Value : NewGear.PurchaseDate);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToPage("/Index");
        }

        // ── Update Gear ───────────────────────────────────────────────────────

        public IActionResult OnPostUpdateGear()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string updateQuery =
                    "UPDATE Gear SET Brand = @Brand, ModelName = @ModelName, " +
                    "Category = @Category, PurchaseDate = @PurchaseDate " +
                    "WHERE GearID = @GearID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GearID", EditGear.GearID);
                    cmd.Parameters.AddWithValue("@Brand",
                        string.IsNullOrEmpty(EditGear.Brand) ? (object)DBNull.Value : EditGear.Brand);
                    cmd.Parameters.AddWithValue("@ModelName",
                        string.IsNullOrEmpty(EditGear.ModelName) ? (object)DBNull.Value : EditGear.ModelName);
                    cmd.Parameters.AddWithValue("@Category",
                        string.IsNullOrEmpty(EditGear.Category) ? (object)DBNull.Value : EditGear.Category);
                    cmd.Parameters.AddWithValue("@PurchaseDate",
                        string.IsNullOrEmpty(EditGear.PurchaseDate) ? (object)DBNull.Value : EditGear.PurchaseDate);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToPage("/Index");
        }

        // ── Load Data Helper ──────────────────────────────────────────────────

        private void LoadData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Gear";
                using (SqlCommand cmd = new SqlCommand(query, conn))
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