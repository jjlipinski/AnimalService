using System.Net;
using System.Text;
using System.Text.Json;
using Npgsql;


// Sample Rest API for animals collection
class AnimalService
{
    // The routes that our service will handle
    public static readonly string[] routes = { 
                                                "http://localhost:8080/"
                                             };

    // Animal object to store user data
    public class Animal {
        public required int Id { get; set; }
        public required string Species { get; set; }
        public required string Habitat { get; set; }
    }

    // PostgreSQL connection string
    public static string connection = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=animals_db";

    // Custom Attribute for Route Handling
    public class HandleRoute : Attribute
    {
        public string Path;
        public string Action;
        public HandleRoute(string action, string path)
        {
            Action = action;
            Path = path;
        }
    }

    /// <summary>
    /// Helper function to add a new habitat value to the habitat database
    /// </summary>
    /// <param name="habitat">The habitat to add</param>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns>awaits a Task object</returns>
    public static async Task addHabitatToDatabase(string habitat, HttpListenerContext context)
    {
        // connect to the database
        await using var dataSource = NpgsqlDataSource.Create(connection);
        // return true if this habitat already exists
        await using (var command = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM Habitats WHERE Habitat = @habitat)"))
        {
            command.Parameters.AddWithValue("habitat", habitat);
            var result = await command.ExecuteScalarAsync();

            // if the habitat doesn't already exist, add it to the table
            if(result is not null && (bool)result == false) 
            {
                await using (var command2 = dataSource.CreateCommand("INSERT INTO Habitats(Habitat) VALUES(@habitat);"))
                {
                    command2.Parameters.AddWithValue("habitat", habitat);
                    await command2.ExecuteNonQueryAsync();
                }
            }
            // if the habitat does exist
            else
            {
                // Duplicate entry code is returned
                context.Response.StatusCode = 409;
                // add a console message too
                Console.WriteLine("This habitat already exists in the database");
            }
        };
    }

    /// <summary>
    /// Helper function to add a new animal to the animal database
    /// </summary>
    /// <param name="animal">The animal to add</param>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns>awaits a Task object</returns>
    public static async Task addAnimalToDatabaseAsync(Animal animal, HttpListenerContext context)
    {
        // connect to the database
        await using var dataSource = NpgsqlDataSource.Create(connection);
        // add a new animal
        await using (var command = dataSource.CreateCommand("INSERT INTO Animals(Species, HabitatId) VALUES(@species,(SELECT Id FROM Habitats WHERE (Habitat = @habitat)));"))
        {
            command.Parameters.AddWithValue("species", animal.Species);
            command.Parameters.AddWithValue("habitat", animal.Habitat);
            var reader = await command.ExecuteNonQueryAsync();
        };
    }

    /// <summary>
    /// Function to delete an animal entry from the animal database
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    [HandleRoute("DELETE", "animal")]
    public async static void HandleDeleteAnimalRoute(HttpListenerContext context)
    {
        // define a reader object to get the animal the user wants to delete
        var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var textResult = await reader.ReadToEndAsync(); 
        
        // make sure the user input is valid first
        if(int.TryParse(textResult, out int animalIdToDelete))
        {
            // connect to the database
            await using var dataSource = NpgsqlDataSource.Create(connection);

            // return true if this animal is in the table
            await using (var command = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM Animals WHERE Animals.Id = @animalId)"))
            {
                command.Parameters.AddWithValue("animalId", animalIdToDelete);
                var result = await command.ExecuteScalarAsync();
                
                // if the animal exists, delete it
                if(result is not null && (bool)result == true) 
                {
                    await using (var command2 = dataSource.CreateCommand("DELETE FROM Animals WHERE Animals.Id = @animalId"))
                    {
                        command2.Parameters.AddWithValue("animalId", animalIdToDelete);
                        await command2.ExecuteNonQueryAsync();

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.OutputStream.Close();
                    }
                }
                else
                {
                    // if we did not find the animal to delete, print error
                    context.Response.StatusCode = 404;
                    Console.WriteLine("Animal not found...returning 404");
                    context.Response.OutputStream.Close();
                }
            };
        }
        else
        {
            // Invalid user input
            context.Response.StatusCode = 404;
            Console.WriteLine("Animal Id not entered");
            context.Response.OutputStream.Close();
        }
    }
    
    /// <summary>
    /// Function to add an animal/habitat entry to the animal/habitat database
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    [HandleRoute("POST", "animals")]
    public async static void HandleAddAnimalRoute(HttpListenerContext context)
    {
        if(context?.Request?.InputStream is not null)
        {
            // define a reader object to get the animal the user wants to add
            var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var textResult = await reader.ReadToEndAsync();

            // This is the animal we want to add
            var animal = JsonSerializer.Deserialize<Animal>(textResult);

            // make sure the animal object is not null and has data
            if(animal is not null && animal.Species != "" && animal.Habitat != "") 
            {
                // connect to the database
                await using var dataSource = NpgsqlDataSource.Create(connection);

                // see if this animal/habitat combo already exists
                await using (var command = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM Animals JOIN Habitats ON Habitats.Id = Animals.HabitatId WHERE Animals.Species = @species AND Habitats.Habitat = @habitat)"))
                {
                    command.Parameters.AddWithValue("@species", animal.Species);
                    command.Parameters.AddWithValue("@habitat", animal.Habitat);
                    var result = await command.ExecuteScalarAsync();
                    
                    if (result is not null && (bool)result == false)
                    {
                        // first add the habitat
                        await addHabitatToDatabase(animal.Habitat, context);

                        // then add the animal
                        await addAnimalToDatabaseAsync(animal, context);

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.OutputStream.Close();
                    }
                    else
                    {
                        // the animal/habitat combo already exists
                        context.Response.StatusCode = 409;
                        Console.WriteLine("This animal already exists");
                        context.Response.OutputStream.Close();
                    }
                }
            }
            else
            {
                // if the animal/habitat combo is empty, show error
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.OutputStream.Close();
            }
        }
    }

    /// <summary>
    /// Handle API request for the "index" route
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    [HandleRoute("GET", "index")]
    public static void HandleIndexRoute(HttpListenerContext context)
    {
        // Navigate up from bin directory to the project root and then to Client/Index.html
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Navigate up 4 levels: bin/Debug/net9.0/AnimalService -> project root
        string filePath = Path.Combine(baseDir, "..", "..", "..", "..", "Client", "Index.html");
        
        // Make sure we have a clean, absolute path
        filePath = Path.GetFullPath(filePath);

        // Check if the file exists
        if (File.Exists(filePath))
        {
            // grab all the bytes from this html file
            byte[] fileBytes = File.ReadAllBytes(filePath);
            Console.WriteLine($"Successfully loaded Index.html from: {filePath}");

            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = fileBytes.Length;
            context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
        }
        else
        {
            // Return error page if file doesn't exist
            string errorMessage = "<HTML><BODY><h1>Error: Index File Not Found</h1>" +
                                 $"<p>The application could not locate the Index.html file at: {filePath}</p>" +
                                 "</BODY></HTML>";
            byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
            
            context.Response.StatusCode = 404;
            context.Response.ContentLength64 = errorBytes.Length;
            context.Response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            
            // Log the error
            Console.WriteLine("Error: Index.html file not found at path: " + filePath);
        }
        
        context.Response.OutputStream.Close();
    }

    /// <summary>
    /// Handle API request for the "animals" route
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    [HandleRoute("GET", "animals")]
    public static async void HandleAnimalsRoute(HttpListenerContext context)
    {
        // create a list of animal objects
        List<Animal> Animals = new List<Animal>();

        Console.WriteLine("Connected to database in HandleAnimalsRoute!");

        int animalsDisplayed = 0;

        // connect to the database
        await using var dataSource = NpgsqlDataSource.Create(connection);
        await using (var command = dataSource.CreateCommand("SELECT Animals.Id, Animals.Species, Habitats.Habitat FROM Animals INNER JOIN Habitats ON Animals.HabitatId = Habitats.Id"))
        {
            await using var reader = await command.ExecuteReaderAsync();

            // if we have species/habitat data to read, turn them into animal objects
            while(await reader.ReadAsync()) 
            { 
                Animals.Add(new Animal 
                {
                    Id = reader.GetInt32(0),
                    Species = char.ToUpper(reader.GetString(1)[0]) + reader.GetString(1).Substring(1).ToLower(),
                    Habitat = char.ToUpper(reader.GetString(2)[0]) + reader.GetString(2).Substring(1).ToLower()
                });
                animalsDisplayed++;
            }

            if(animalsDisplayed == 0)
            {
                context.Response.StatusCode = 404;
                Console.WriteLine("No animals in the table");
                context.Response.OutputStream.Close();
            }
            else
            {
                // display buffer of animal data
                var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Animals));
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
        };
    }

    /// <summary>
    /// Handle API request to get a single animal from the "animal" route
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    [HandleRoute("GET", "animal")]
    public static async void HandleAnimalRoute(HttpListenerContext context)
    {
        // the animal we will serialize
        Animal? animal = null;

        if (context?.Request?.Url != null) 
        {
            // Animal route requires exactly one paramter which is the requested ID
            //  of the animal to return
            if (context.Request.Url.Segments.Length == 3)
            {
                // Get the requested animal ID from the URL
                int requestedId = int.TryParse(context.Request.Url.Segments[2].Replace("/", ""), out requestedId) ? requestedId : 0;

                Console.WriteLine("Connected to database in HandleAnimalRoute!");

                // connect to the database
                await using var dataSource = NpgsqlDataSource.Create(connection);

                // first we are going to check if the animal we want to display exists
                await using (var command = dataSource.CreateCommand("SELECT exists(SELECT 1 FROM Animals WHERE Animals.Id = @requestedId);"))
                {
                    command.Parameters.AddWithValue("requestedId", requestedId);
                    var result = await command.ExecuteScalarAsync();

                    // if the animal is found, retrieve it
                    if(result is not null && (bool)result == true) 
                    {
                        await using (var command2 = dataSource.CreateCommand("SELECT Animals.Id, Animals.Species, Habitats.Habitat FROM Animals INNER JOIN Habitats ON Animals.HabitatId = Habitats.Id WHERE Animals.Id = @requestedId"))
                        {
                            command2.Parameters.AddWithValue("@requestedId", requestedId);
                            await using var reader = await command2.ExecuteReaderAsync();

                            if (await reader.ReadAsync()) 
                            {
                                animal = new Animal 
                                {
                                    Id = reader.GetInt32(0),
                                    Species = char.ToUpper(reader.GetString(1)[0]) + reader.GetString(1).Substring(1).ToLower(),
                                    Habitat = char.ToUpper(reader.GetString(2)[0]) + reader.GetString(2).Substring(1).ToLower()
                                };
                            }
                            else
                            {
                                // if there is no species/habitat data to read, show an error
                                context.Response.StatusCode = 404;
                                Console.WriteLine("Animal not found...returning 404");
                                context.Response.OutputStream.Close();
                            }
                        }

                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(animal));
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                    else
                    {
                        // animal was not found so show an error
                        context.Response.StatusCode = 404;
                        Console.WriteLine("Animal not found...returning 404");
                        context.Response.OutputStream.Close();
                    }
                };
            }
            else
            {
                // if there was no id provided to look up send error response
                context.Response.StatusCode = 400;
                var buffer = Encoding.UTF8.GetBytes("<HTML><BODY><h1>Bad Request Error</h1><span>Animal requries exactly one parameter (animal id)</span></BODY></HTML>");
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
        }
    }

    /// <summary>
    /// Process and Handle actual requests to the service
    /// </summary>
    /// <param name="context">Provides acces to http request/response object</param>
    /// <returns></returns>
    public static void ProcessRequest(HttpListenerContext context)
    {
        try {
            if (context?.Request?.Url != null) {

                // Get our route from the URL
                String route = context.Request.Url.Segments[1].Replace("/", "");

                // Get the action from the request
                String action = context.Request.HttpMethod;

                // Get the first method in this class that have HandleRoute attribute where the path is equal
                //  to the path in the request
                var method = typeof(AnimalService)
                                .GetMethods()
                                .Where(method => method.GetCustomAttributes(true).Any(attr => attr is HandleRoute && ((HandleRoute)attr).Path == route && ((HandleRoute)attr).Action == action))
                                .FirstOrDefault();

                if(method is not null)
                {
                    // Call that method...if we have one!
                    method.Invoke(null, new object[]{context});
                }
                else
                {
                    Console.WriteLine($"{route} not found...returning 404");
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                }
            }
        } 
        catch (Exception e) 
        {
            //Return the exception details the client
            context.Response.StatusCode = 500;
            var buffer = Encoding.UTF8.GetBytes($"<HTML><BODY><h1>Internal Server Error</h1><span>{e.Message}</span></BODY></HTML>");
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
    
    /// <summary>
    /// The main loop to handle requests into the HttpListener
    /// </summary>
    /// <param name="listener">Http protocol listener object</param>
    /// <returns></returns>
    public static async Task MainLoop(HttpListener listener) {
        listener.Start();
        while (true) {
            try {
                //GetContextAsync() returns when a new request come in
                var context = await listener.GetContextAsync();
                lock (listener) {
                    ProcessRequest(context);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }
        }
    }

    /// <summary>
    /// Main entry point for service
    /// </summary>
    /// <returns></returns>
    public static void Main()
    {
        // Create a listener.
        HttpListener listener = new HttpListener();
        // Add the prefixes.
        foreach (string s in routes)
        {
            listener.Prefixes.Add(s);
        }

        Console.WriteLine("Listening...");
        Task _mainLoop = MainLoop(listener);
        while(!_mainLoop.IsCompleted)
        {
            // Empty Loop
        }
        
        listener.Stop();
    }
}
