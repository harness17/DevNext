using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Identity;
using PdfSample.Data;
using PdfSample.Entity;

namespace PdfSample.Common;

public static class PdfSampleSeeder
{
    public static async Task SeedAsync(
        PdfSampleDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new ApplicationRole { Id = "1", Name = "Admin" });

        if (!await roleManager.RoleExistsAsync("Member"))
            await roleManager.CreateAsync(new ApplicationRole { Id = "2", Name = "Member" });

        if (await userManager.FindByNameAsync("admin1@sample.jp") == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = "1",
                Email = "admin1@sample.jp",
                UserName = "admin1@sample.jp",
                ApplicationRoleName = "Admin"
            };
            await userManager.CreateAsync(adminUser, "Admin1!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        if (await userManager.FindByNameAsync("member1@sample.jp") == null)
        {
            var memberUser = new ApplicationUser
            {
                Id = "2",
                Email = "member1@sample.jp",
                UserName = "member1@sample.jp",
                ApplicationRoleName = "Member"
            };
            await userManager.CreateAsync(memberUser, "Member1!");
            await userManager.AddToRoleAsync(memberUser, "Member");
        }

        if (context.InvoiceEntity.Any()) return;

        var invoices = new List<InvoiceEntity>
        {
            new()
            {
                InvoiceNumber = "INV-202604-001",
                ClientName = "株式会社サンプル商事",
                IssueDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                Status = InvoiceStatus.Draft,
                CreateApplicationUserId = "1",
                UpdateApplicationUserId = "1",
                Notes = "4月分の保守費用です。",
                Items =
                [
                    new InvoiceItemEntity { Description = "Web保守作業", Quantity = 1, UnitPrice = 120000m },
                    new InvoiceItemEntity { Description = "サーバー監視", Quantity = 1, UnitPrice = 30000m }
                ]
            },
            new()
            {
                InvoiceNumber = "INV-202604-002",
                ClientName = "有限会社東京デザイン",
                IssueDate = new DateTime(2026, 4, 2),
                DueDate = new DateTime(2026, 5, 2),
                Status = InvoiceStatus.Issued,
                CreateApplicationUserId = "2",
                UpdateApplicationUserId = "2",
                Notes = "デザイン改善案件。",
                Items =
                [
                    new InvoiceItemEntity { Description = "UI改善提案", Quantity = 1, UnitPrice = 80000m },
                    new InvoiceItemEntity { Description = "画面デザイン制作", Quantity = 2, UnitPrice = 50000m },
                    new InvoiceItemEntity { Description = "デザインレビュー", Quantity = 1, UnitPrice = 25000m }
                ]
            },
            new()
            {
                InvoiceNumber = "INV-202604-003",
                ClientName = "合同会社北海システム",
                IssueDate = new DateTime(2026, 4, 3),
                DueDate = new DateTime(2026, 5, 10),
                Status = InvoiceStatus.Paid,
                CreateApplicationUserId = "2",
                UpdateApplicationUserId = "2",
                Notes = "導入支援完了分。",
                Items =
                [
                    new InvoiceItemEntity { Description = "導入支援", Quantity = 3, UnitPrice = 40000m },
                    new InvoiceItemEntity { Description = "操作説明会", Quantity = 1, UnitPrice = 35000m }
                ]
            }
        };

        foreach (var invoice in invoices)
        {
            invoice.SetForCreate();
            foreach (var item in invoice.Items)
                item.SetForCreate();
        }

        context.InvoiceEntity.AddRange(invoices);
        await context.SaveChangesAsync();
    }
}
