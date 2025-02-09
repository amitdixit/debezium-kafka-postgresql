using System.Text.Json;
using Bogus;
using Dapper;
using Npgsql;

namespace generator;

public class Worker : BackgroundService
{
    static readonly string _writeConnectionString = @"Server=localhost;Port=5432;Database=mydatabase;User Id=myuser;Password=mypassword;";

    static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var item = BogusDataGenerator.GetSampleTableData(1).FirstOrDefault();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);


                using (var dbConnection = new NpgsqlConnection(_writeConnectionString))
                {
                    try
                    {
                        var insertedUser = await dbConnection.QueryFirstAsync<TableUserExtended>($"INSERT INTO public.users (customerid, modifieddate, title, firstname, lastname, middlename, namestyle, suffix, companyname, salesperson, emailaddress, phone) " +
                               $" VALUES (@customerid, @modifieddate, @title, @firstname, @lastname, @middlename, @namestyle, @suffix, @companyname, @salesperson, @emailaddress, @phone) RETURNING *,cast ( inet_server_addr() as text) server;",
                               new
                               {
                                   customerid = item.CustomerId,
                                   modifieddate = DateTime.Now,
                                   title = item.Title,
                                   firstname = item.FirstName,
                                   lastname = item.LastName,
                                   middlename = item.MiddleName,
                                   namestyle = item.NameStyle,
                                   suffix = item.Suffix,
                                   companyname = item.CompanyName,
                                   salesperson = item.SalesPerson,
                                   emailaddress = item.EmailAddress,
                                   phone = item.Phone
                               }, commandType: System.Data.CommandType.Text);

                        await Task.Delay(1000);
                        Console.ForegroundColor = ConsoleColor.Green;
                        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(insertedUser, _options));
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {

                    }
                }


            }
            // await Task.Delay(1000, stoppingToken);
        }
    }
}



public class PgSqlHelper
{
    static readonly string _writeConnectionString = @"WRITE CONNECTION STRING";
    static readonly string _readConnectionString = @"READ CONNECTION STRING";

    static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task InsertData()
    {
        var data = BogusDataGenerator.GetSampleTableData(10000);


        foreach (var item in data)
        {
            using (var dbConnection = new NpgsqlConnection(_writeConnectionString))
            {
                try
                {
                    var insertedUser = await dbConnection.QueryFirstAsync<TableUserExtended>($"INSERT INTO public.users (customerid, modifieddate, title, firstname, lastname, middlename, namestyle, suffix, companyname, salesperson, emailaddress, phone) " +
                           $" VALUES (@customerid, @modifieddate, @title, @firstname, @lastname, @middlename, @namestyle, @suffix, @companyname, @salesperson, @emailaddress, @phone) RETURNING *,cast ( inet_server_addr() as text) server;",
                           new
                           {
                               customerid = item.CustomerId,
                               modifieddate = DateTime.Now,
                               title = item.Title,
                               firstname = item.FirstName,
                               lastname = item.LastName,
                               middlename = item.MiddleName,
                               namestyle = item.NameStyle,
                               suffix = item.Suffix,
                               companyname = item.CompanyName,
                               salesperson = item.SalesPerson,
                               emailaddress = item.EmailAddress,
                               phone = item.Phone
                           }, commandType: System.Data.CommandType.Text);

                    await Task.Delay(300);
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Console.Out.WriteLineAsync(JsonSerializer.Serialize(insertedUser, _options));
                }
                catch (Exception ex)
                {

                }
                finally
                {

                }
            }
        }


    }


    public static async Task ReadData()
    {

        var sqlQuery = $"SELECT *,cast (inet_server_addr() as text) server FROM public.users u order by u.modifieddate desc limit 1;";
        var sqlQuery1 = $"SELECT *,cast ( inet_server_addr() as text) server FROM public.users where customerid=@customerid;";
        while (true)
        {

            using (var dbConnection = new NpgsqlConnection(_readConnectionString))
            {
                try
                {
                    var record = await dbConnection.QueryFirstAsync<TableUserExtended>(sqlQuery,
                           new
                           {
                               // customerid = Random.Shared.Next(1, 2500),
                           }, commandType: System.Data.CommandType.Text);

                    await Task.Delay(200);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    await Console.Out.WriteLineAsync(JsonSerializer.Serialize(record, _options));
                }
                catch (Exception ex)
                {

                    // throw;
                }
                finally
                {

                }
            }
        }

    }
}



public static class BogusDataGenerator
{
    private static readonly Random _random = new();
    const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static List<TableUser> GetSampleTableData(int numberOfRecords = 100000)
    {
        var customerId = 1;
        var userFaker = new Faker<TableUser>("en_IND")
            .CustomInstantiator(f => new TableUser(customerId++.ToString()))
            .RuleFor(o => o.CustomerCode, f => RandomString(10))
            .RuleFor(o => o.ModifiedDate, f => f.Date.Recent(100))
            .RuleFor(o => o.NameStyle, f => f.Random.Bool())
            .RuleFor(o => o.Phone, f => f.Person.Phone)
            .RuleFor(o => o.FirstName, f => f.Name.FirstName())
            .RuleFor(o => o.LastName, f => f.Name.LastName())
            .RuleFor(o => o.Title, f => f.Name.Prefix(f.Person.Gender))
            .RuleFor(o => o.Suffix, f => f.Name.Suffix())
            .RuleFor(o => o.MiddleName, f => f.Name.FirstName())
            .RuleFor(o => o.EmailAddress, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(o => o.SalesPerson, f => f.Name.FullName())
            .RuleFor(o => o.CompanyName, f => f.Company.CompanyName());

        return userFaker.Generate(numberOfRecords);
    }

    public static string RandomString(int length) => new(Enumerable.Repeat(_chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());

}

public class TableUser
{
    private string _customerId;

    public TableUser() { }

    public TableUser(string customerId)
    {
        _customerId = customerId;

    }
    public string CustomerId
    {
        get => _customerId;
        set => _customerId = value;
    }
    public DateTime ModifiedDate { get; set; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public bool NameStyle { get; set; }
    public string Suffix { get; set; }
    public string CompanyName { get; set; }
    public string SalesPerson { get; set; }
    public string EmailAddress { get; set; }
    public string Phone { get; set; }
    public string CustomerCode { get; set; }
}

public class TableUserExtended : TableUser
{
    public string Server { get; set; }

}