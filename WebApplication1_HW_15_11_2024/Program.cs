//Создать класс «User». Определить интерфейс и репозиторий по управлению пользователями,
//с доступными действиями: добавить, удалить, получить конкретного пользователя,
//редактировать, вывести всех пользователей. 
//Создать веб-сайт по управлению этими пользователями на несколько страниц 
//(без базы данных, использовать подходящий жизненный цикл сервиса для полноценной работы в одном сеансе).
//Весь код можно писать в классе Program.cs или использовать отдельные представления.
//Обработать возможные ошибочные ситуации, к примеру передачу неверного Id (как в формате так и в плане существования).

using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<IUserManager, UserManager>();

var app = builder.Build();


app.MapGet("/", async (IUserManager userManager) =>
{
    var users = userManager.GetAllUsers();
    var htmlContent = HtmlPageCreator.GenerateHtmlPage(HtmlPageCreator.BuildHtmlTable(users), "All Users");
    return Results.Text(htmlContent, "text/html");
});

app.MapPost("/add", async (HttpRequest request, IUserManager userManager) =>
{
    var form = await request.ReadFormAsync();
    string name = form["name"];
    int age = int.Parse(form["age"]);
    string email = form["email"];

    userManager.AddUser(new User { Name = name, Age = age, Email = email });
    return Results.Redirect("/");
});

app.MapPost("/delete", (HttpRequest request, IUserManager userManager) =>
{
    int id = int.Parse(request.Form["id"]);
    try
    {
        userManager.RemoveUser(id);
    }
    catch (KeyNotFoundException)
    {
        return Results.BadRequest($"User with ID {id} not found.");
    }
    return Results.Redirect("/");
});

app.MapPost("/edit", async (HttpRequest request, IUserManager userManager) =>
{
    var form = await request.ReadFormAsync();
    int id = int.Parse(form["id"]);
    string name = form["name"];
    int age = int.Parse(form["age"]);
    string email = form["email"];

    try
    {
        userManager.EditUser(id, new User { Name = name, Age = age, Email = email });
    }
    catch (KeyNotFoundException)
    {
        return Results.BadRequest($"User with ID {id} not found.");
    }
    return Results.Redirect("/");
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

public interface IUserManager
{
    void AddUser(User user);
    void RemoveUser(int id);
    User GetUserById(int id);
    void EditUser(int id, User updatedUser);
    IEnumerable<User> GetAllUsers();
}

public class UserManager : IUserManager
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public void AddUser(User user)
    {
        user.Id = _nextId++;
        _users.Add(user);
    }

    public void RemoveUser(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null) throw new KeyNotFoundException($"User with ID {id} not found.");
        _users.Remove(user);
    }

    public User GetUserById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null) throw new KeyNotFoundException($"User with ID {id} not found.");
        return user;
    }

    public void EditUser(int id, User updatedUser)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null) throw new KeyNotFoundException($"User with ID {id} not found.");

        user.Name = updatedUser.Name;
        user.Age = updatedUser.Age;
        user.Email = updatedUser.Email;
    }

    public IEnumerable<User> GetAllUsers() => _users;
}

public static class HtmlPageCreator
{
    public static string GenerateHtmlPage(string body, string header)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0-alpha3/dist/css/bootstrap.min.css" rel="stylesheet">
                <title>{header}</title>
            </head>
            <body>
                <div class="container mt-3">
                    <h1>{header}</h1>
                    <form action="/add" method="POST" class="mb-3">
                        <input name="name" placeholder="Name" required class="form-control mb-1" />
                        <input name="age" placeholder="Age" type="number" required class="form-control mb-1" />
                        <input name="email" placeholder="Email" required class="form-control mb-1" />
                        <button type="submit" class="btn btn-primary">Add User</button>
                    </form>
                    <form action="/delete" method="POST" class="mb-3">
                        <input name="id" placeholder="User ID to Delete" required class="form-control mb-1" />
                        <button type="submit" class="btn btn-danger">Delete User</button>
                    </form>
                    <form action="/edit" method="POST">
                        <input name="id" placeholder="User ID to Edit" required class="form-control mb-1" />
                        <input name="name" placeholder="New Name" required class="form-control mb-1" />
                        <input name="age" placeholder="New Age" type="number" required class="form-control mb-1" />
                        <input name="email" placeholder="New Email" required class="form-control mb-1" />
                        <button type="submit" class="btn btn-warning">Edit User</button>
                    </form>
                    {body}
                </div>
            </body>
            </html>
        """;
    }

    public static string BuildHtmlTable(IEnumerable<User> users)
    {
        if (!users.Any())
            return "<p>No users found.</p>";

        var rows = string.Join("\n", users.Select(user => $"""
            <tr>
                <td>{user.Id}</td>
                <td>{user.Name}</td>
                <td>{user.Age}</td>
                <td>{user.Email}</td>
            </tr>
        """));

        return $"""
            <table class="table table-striped">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Name</th>
                        <th>Age</th>
                        <th>Email</th>
                    </tr>
                </thead>
                <tbody>
                    {rows}
                </tbody>
            </table>
        """;
    }
}
