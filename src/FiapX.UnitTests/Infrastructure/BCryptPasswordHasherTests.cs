using FiapX.Infrastructure.Services;
using FluentAssertions;

namespace FiapX.UnitTests.Infrastructure
{
    public class BCryptPasswordHasherTests
    {
        private readonly BCryptPasswordHasher _hasher;

        public BCryptPasswordHasherTests()
        {
            _hasher = new BCryptPasswordHasher();
        }

        [Fact]
        public void Hash_Should_Generate_Valid_BCrypt_Hash()
        {
            var password = "mySecretPassword";

            var hash = _hasher.Hash(password);

            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password);

            hash.Should().StartWith("$2");
            hash.Length.Should().Be(60);
        }

        [Fact]
        public void Verify_Should_Return_True_For_Correct_Password()
        {
            var password = "password123";
            var hash = _hasher.Hash(password);

            var result = _hasher.Verify(password, hash);

            result.Should().BeTrue();
        }

        [Fact]
        public void Verify_Should_Return_False_For_Incorrect_Password()
        {
            var password = "password123";
            var hash = _hasher.Hash(password);
            var wrongPassword = "wrongPassword";

            var result = _hasher.Verify(wrongPassword, hash);

            result.Should().BeFalse();
        }

        [Fact]
        public void Hash_Should_Generate_Different_Hashes_For_Same_Password()
        {
            var password = "samePassword";

            var hash1 = _hasher.Hash(password);
            var hash2 = _hasher.Hash(password);

            hash1.Should().NotBe(hash2);

            _hasher.Verify(password, hash1).Should().BeTrue();
            _hasher.Verify(password, hash2).Should().BeTrue();
        }
    }
}
