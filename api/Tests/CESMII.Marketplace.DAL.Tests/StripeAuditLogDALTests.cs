using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Repositories;
using Moq;

namespace CESMII.Marketplace.DAL.Tests
{
    public class StripeAuditLogDALTests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public async Task Add_ShouldReturnId_WhenModelIsValid()
        {
            // Arrange
            var id = "123";
            var model = new StripeAuditLogModel { ID = id, Type = "TestType", AdditionalInfo = "TestInfo", Created = DateTime.UtcNow, Message = "TestMessage" };
            var userId = "65f1dd3ce85607992106e0ef";

            var _mockRepo = new Mock<IMongoRepository<StripeAuditLog>>();
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<StripeAuditLog>())).ReturnsAsync(model.ID);

            var _stripeAuditLogDAL = new StripeAuditLogDAL(_mockRepo.Object);
                       

            // Act
            var result = await _stripeAuditLogDAL.Add(model, userId);

            // Assert
            Assert.AreEqual(id, result);
        }
    }
}
