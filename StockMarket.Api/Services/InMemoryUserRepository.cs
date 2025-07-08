// StockMarket.Api/Services/InMemoryUserRepository.cs
using StockMarket.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;


namespace StockMarket.Api.Services
{
    public class InMemoryUserRepository : IUserRepository
    {
        // _users listesini instance bazlı yapıp, servisi Singleton olarak kaydetmek daha yaygın bir pratiktir
        // Ancak static liste ve static _fileLock ile de basit senaryolarda çalışır.
        // Eğer servis Scoped veya Transient ise _users kesinlikle static olmalı ya da
        // her istekte dosyadan okuma/yazma yapılmalı ki bu verimsiz olur.
        // Program.cs'de Singleton olarak kaydettiğimiz için static olmasa da olurdu.
        // Basitlik ve tutarlılık için static bırakabiliriz veya instance bazlı yapıp constructor'da initialize edebiliriz.
        // Şimdilik static bırakalım.
        private static List<User> _users = new List<User>();
        private readonly string _filePath;
        private readonly ILogger<InMemoryUserRepository> _logger;
        private static readonly object _fileLock = new object(); // Dosya işlemleri için basit bir kilit

        public InMemoryUserRepository(IWebHostEnvironment env, ILogger<InMemoryUserRepository> logger)
        {
            _filePath = Path.Combine(env.ContentRootPath, "users.json");
            _logger = logger;
            // Uygulama başladığında sadece bir kez kullanıcıları yükle
            // Eğer _users listesi zaten doluysa (örneğin, testlerde veya farklı bir senaryoda birden fazla instance oluşursa)
            // tekrar yüklememek için bir kontrol eklenebilir, ama Singleton servis için bu gerekmeyebilir.
            // Şimdilik her instance oluşturulduğunda (Singleton olduğu için bir kez) yüklüyoruz.
            LoadUsersFromFile();
        }

        private void LoadUsersFromFile()
        {
            lock (_fileLock)
            {
                bool saveNeededAfterLoad = false;
                try
                {
                    if (File.Exists(_filePath))
                    {
                        var jsonData = File.ReadAllText(_filePath);
                        if (!string.IsNullOrWhiteSpace(jsonData))
                        {
                            _users = JsonSerializer.Deserialize<List<User>>(jsonData) ?? new List<User>();
                            _logger.LogInformation("{Count} kullanıcı users.json dosyasından yüklendi.", _users.Count);
                        }
                        else
                        {
                            _users = new List<User>(); // Dosya boşsa yeni liste
                            _logger.LogInformation("users.json dosyası boş, yeni bir kullanıcı listesi oluşturuldu.");
                            saveNeededAfterLoad = true; // Boş dosyaya varsayılanlar yazılacak
                        }
                    }
                    else
                    {
                        _users = new List<User>(); // Dosya yoksa yeni liste
                        _logger.LogInformation("users.json dosyası bulunamadı, yeni bir kullanıcı listesi oluşturuldu.");
                        saveNeededAfterLoad = true; // Yok olan dosyaya varsayılanlar yazılacak
                    }

                    // Varsayılan admin ve test kullanıcısını kontrol et ve yoksa ekle
                    // EnsureDefaultUser metodu zaten _users listesini güncelliyor.
                    bool defaultAdminAdded = EnsureDefaultUser("admin", "adminpass", "Admin");
                    bool defaultTestUserAdded = EnsureDefaultUser("testuser", "testpass", "User");

                    // Eğer varsayılan kullanıcılar yeni eklendiyse veya dosya boş/yok idiyse kaydet
                    if (saveNeededAfterLoad || defaultAdminAdded || defaultTestUserAdded)
                    {
                        SaveChangesToFile();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "users.json dosyasından kullanıcılar yüklenirken hata oluştu. Varsayılan kullanıcılar oluşturuluyor.");
                    _users.Clear(); // Hata durumunda listeyi temizle, sadece varsayılanları ekle
                    EnsureDefaultUser("admin", "adminpass", "Admin");
                    EnsureDefaultUser("testuser", "testpass", "User");
                    SaveChangesToFile(); // Hata durumunda varsayılanları kaydet
                }
            }
        }

        // Bu metot artık bool dönecek, ekleme yapılıp yapılmadığını belirtmek için
        private bool EnsureDefaultUser(string username, string password, string role)
        {
            if (!_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                _users.Add(new User // User constructor'ı zaten Id atıyor
                {
                    Username = username,
                   PasswordHash = global::BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role
                });
                _logger.LogInformation("Varsayılan kullanıcı '{Username}' (Rol: {Role}) oluşturuldu ve listeye eklendi.", username, role);
                return true; // Ekleme yapıldı
            }
            return false; // Zaten vardı, ekleme yapılmadı
        }

        private void SaveChangesToFile()
        {
            lock (_fileLock)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var jsonData = JsonSerializer.Serialize(_users, options);
                    File.WriteAllText(_filePath, jsonData);
                    _logger.LogInformation("Kullanıcı değişiklikleri ({Count} kullanıcı) users.json dosyasına kaydedildi.", _users.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kullanıcı değişiklikleri users.json dosyasına kaydedilirken hata oluştu.");
                }
            }
        }

        public Task AddUserAsync(User user)
        {
            // User sınıfının constructor'ı zaten Id atıyor.
            if (_users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("'{Username}' kullanıcı adı zaten mevcut, eklenemedi.", user.Username);
                // Controller'a bu durumu bildirmek için bir exception fırlatılabilir veya bool dönülebilir.
                // Şimdilik sadece logluyoruz ve işlem yapmıyoruz.
                // AuthController zaten bu durumu ayrıca kontrol ediyor.
                return Task.CompletedTask;
            }

            _users.Add(user);
            SaveChangesToFile();
            _logger.LogInformation("Kullanıcı '{Username}' başarıyla eklendi ve dosyaya kaydedildi.", user.Username);
            return Task.CompletedTask;
        }

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                _logger.LogDebug("Kullanıcı '{Username}' bulunamadı.", username);
            }
            return Task.FromResult(user);
        }
    }
}