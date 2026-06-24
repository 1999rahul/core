// ============================================================================
// DECORATOR PATTERN - Detailed Explanation & Real-World Example
// ============================================================================
//
// WHAT IS IT?
// The Decorator Pattern is a STRUCTURAL design pattern that lets you attach
// new behavior to an object dynamically by wrapping it in "decorator"
// objects, without modifying the original object's code.
//
// WHY USE IT?
// Without it, adding combinations of behavior via inheritance causes a
// combinatorial explosion of subclasses (e.g. EncryptedFile, CompressedFile,
// EncryptedAndCompressedFile, EncryptedCompressedScannedFile, ...).
// Decorator solves this by composing small, independent wrapper classes
// at runtime instead of baking combinations into the class hierarchy.
//
// STRUCTURE:
//   1. Component         -> common interface shared by core object & decorators
//   2. Concrete Component -> the base/core object being decorated
//   3. Decorator (abstract) -> implements Component, holds a wrapped Component
//   4. Concrete Decorators  -> add one specific behavior each
//
// REAL-WORLD ANALOGY USED BELOW:
// A FILE PROCESSING PIPELINE (e.g. document management / cloud upload system).
// Files may need: virus scanning, compression, encryption, audit logging —
// but not every file needs every step, and different use cases need
// different combinations and ORDER of steps.
//
// SOLID CONNECTIONS:
//   - Open/Closed Principle: extend behavior without modifying existing classes.
//   - Single Responsibility: each decorator owns exactly one concern.
//
// DECORATOR vs OTHER PATTERNS (common interview follow-up):
//   - vs Adapter  -> Adapter changes an interface to fit; Decorator keeps the
//                    same interface and ADDS behavior.
//   - vs Proxy    -> Proxy controls ACCESS (lazy load, security checks);
//                    Decorator ADDS RESPONSIBILITIES. Structurally similar,
//                    different intent.
//   - vs Strategy -> Strategy swaps an entire algorithm; Decorator layers
//                    additional behavior on top of existing behavior.
//
// REAL .NET FRAMEWORK EXAMPLES OF THIS EXACT PATTERN:
//   - System.IO.Stream wrapping: GZipStream, CryptoStream, BufferedStream
//     all wrap a base Stream and add one capability each.
//   - ASP.NET Core middleware pipeline is conceptually a decorator chain.
//
// TRADE-OFFS:
//   - Can create many small classes / deep wrapping chains that are harder
//     to debug.
//   - Order of decoration can matter and change behavior (see Main() below).
//   - A decorated object is not type-identical to the original — code that
//     does type-checking on the concrete type can break.
// ============================================================================

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace DecoratorPatternDemo
{
    // ------------------------------------------------------------------
    // 1. COMPONENT
    // Defines the common interface that the core object and every
    // decorator must implement. Client code only ever talks to this.
    // ------------------------------------------------------------------
    public interface IFileProcessor
    {
        byte[] Process(byte[] fileData);
        string GetProcessingSummary();
    }

    // ------------------------------------------------------------------
    // 2. CONCRETE COMPONENT
    // The base object being decorated. Contains the "core" behavior
    // that decorators will wrap and extend.
    // ------------------------------------------------------------------
    public class BasicFileProcessor : IFileProcessor
    {
        public byte[] Process(byte[] fileData)
        {
            // Base case: no processing, just pass the raw bytes through.
            return fileData;
        }

        public string GetProcessingSummary() => "Raw file";
    }

    // ------------------------------------------------------------------
    // 3. BASE DECORATOR (abstract)
    // Implements the same interface as the component AND holds a
    // reference to a wrapped IFileProcessor. By default it just
    // forwards calls to the wrapped object — concrete decorators
    // override to inject their own behavior before/after forwarding.
    // ------------------------------------------------------------------
    public abstract class FileProcessorDecorator : IFileProcessor
    {
        protected readonly IFileProcessor _wrappedProcessor;

        protected FileProcessorDecorator(IFileProcessor processor)
        {
            _wrappedProcessor = processor;
        }

        public virtual byte[] Process(byte[] fileData) => _wrappedProcessor.Process(fileData);

        public virtual string GetProcessingSummary() => _wrappedProcessor.GetProcessingSummary();
    }

    // ------------------------------------------------------------------
    // 4. CONCRETE DECORATORS
    // Each one adds exactly ONE concern (Single Responsibility) and
    // can be combined with any of the others, in any order.
    // ------------------------------------------------------------------

    /// <summary>
    /// Adds GZip compression. NOTE on ordering: compression should
    /// generally happen BEFORE encryption, since encrypted data looks
    /// like random noise and does not compress well.
    /// </summary>
    public class CompressionDecorator : FileProcessorDecorator
    {
        public CompressionDecorator(IFileProcessor processor) : base(processor) { }

        public override byte[] Process(byte[] fileData)
        {
            byte[] processed = _wrappedProcessor.Process(fileData);
            return Compress(processed);
        }

        public override string GetProcessingSummary() =>
            $"{_wrappedProcessor.GetProcessingSummary()} -> Compressed";

        private static byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    /// <summary>
    /// Adds AES encryption. The generated IV is prepended to the
    /// ciphertext so it's available later for decryption.
    /// </summary>
    public class EncryptionDecorator : FileProcessorDecorator
    {
        private readonly byte[] _key;

        public EncryptionDecorator(IFileProcessor processor, byte[] key) : base(processor)
        {
            _key = key;
        }

        public override byte[] Process(byte[] fileData)
        {
            byte[] processed = _wrappedProcessor.Process(fileData);
            return Encrypt(processed);
        }

        public override string GetProcessingSummary() =>
            $"{_wrappedProcessor.GetProcessingSummary()} -> Encrypted";

        private byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            return aes.IV.Concat(encrypted).ToArray();
        }
    }

    /// <summary>
    /// Simulates a virus scan. In a real system this would call out to
    /// an actual AV engine/API. Throws if a threat is detected, which
    /// short-circuits the rest of the pipeline.
    /// </summary>
    public class VirusScanDecorator : FileProcessorDecorator
    {
        public VirusScanDecorator(IFileProcessor processor) : base(processor) { }

        public override byte[] Process(byte[] fileData)
        {
            if (ContainsThreat(fileData))
                throw new InvalidOperationException("File failed virus scan.");

            return _wrappedProcessor.Process(fileData);
        }

        public override string GetProcessingSummary() =>
            $"{_wrappedProcessor.GetProcessingSummary()} -> Virus Scanned";

        private static bool ContainsThreat(byte[] data) => false; // plug in a real AV engine here
    }

    /// <summary>
    /// Adds audit logging around whatever it wraps — logs before and
    /// after delegating to the wrapped processor.
    /// </summary>
    public class AuditLogDecorator : FileProcessorDecorator
    {
        public AuditLogDecorator(IFileProcessor processor) : base(processor) { }

        public override byte[] Process(byte[] fileData)
        {
            Console.WriteLine($"[AUDIT] Processing started at {DateTime.UtcNow:O}, input size: {fileData.Length} bytes");
            var result = _wrappedProcessor.Process(fileData);
            Console.WriteLine($"[AUDIT] Processing completed at {DateTime.UtcNow:O}, output size: {result.Length} bytes");
            return result;
        }

        public override string GetProcessingSummary() =>
            $"{_wrappedProcessor.GetProcessingSummary()} -> Audit Logged";
    }

    // ------------------------------------------------------------------
    // DEMO / USAGE
    // Shows two different real-world compositions built from the same
    // set of decorator building blocks.
    // ------------------------------------------------------------------
    public static class Program
    {
        public static void Main()
        {
            byte[] rawFileBytes = System.Text.Encoding.UTF8.GetBytes(
                "This represents the raw bytes of an uploaded file.");

            // Scenario 1: Medical records.
            // Compliance requires virus scanning, encryption, and an audit
            // trail. Order matters here: scan first (reject bad files
            // early), then encrypt, then log around the whole thing.
            byte[] encryptionKey = Aes.Create().Key; // 256-bit key for demo purposes

            IFileProcessor medicalFileProcessor = new BasicFileProcessor();
            medicalFileProcessor = new VirusScanDecorator(medicalFileProcessor);
            medicalFileProcessor = new EncryptionDecorator(medicalFileProcessor, encryptionKey);
            medicalFileProcessor = new AuditLogDecorator(medicalFileProcessor);

            Console.WriteLine("=== Medical Record Pipeline ===");
            byte[] medicalResult = medicalFileProcessor.Process(rawFileBytes);
            Console.WriteLine($"Pipeline: {medicalFileProcessor.GetProcessingSummary()}");
            Console.WriteLine($"Final size: {medicalResult.Length} bytes");
            Console.WriteLine();

            // Scenario 2: Marketing assets.
            // No compliance requirements — just needs compression to save
            // storage space. Demonstrates how the SAME building blocks
            // recombine differently for a different use case.
            IFileProcessor marketingFileProcessor = new BasicFileProcessor();
            marketingFileProcessor = new CompressionDecorator(marketingFileProcessor);

            Console.WriteLine("=== Marketing Asset Pipeline ===");
            byte[] marketingResult = marketingFileProcessor.Process(rawFileBytes);
            Console.WriteLine($"Pipeline: {marketingFileProcessor.GetProcessingSummary()}");
            Console.WriteLine($"Final size: {marketingResult.Length} bytes");
            Console.WriteLine();

            // Scenario 3: Why ORDER matters.
            // Compressing AFTER encrypting is wasteful: encrypted bytes are
            // effectively random noise and won't compress well. Compressing
            // BEFORE encrypting is the correct order.
            IFileProcessor wrongOrder = new BasicFileProcessor();
            wrongOrder = new EncryptionDecorator(wrongOrder, encryptionKey);
            wrongOrder = new CompressionDecorator(wrongOrder); // compressing already-encrypted data

            IFileProcessor rightOrder = new BasicFileProcessor();
            rightOrder = new CompressionDecorator(rightOrder);
            rightOrder = new EncryptionDecorator(rightOrder, encryptionKey); // encrypt last

            Console.WriteLine("=== Demonstrating Why Order Matters ===");
            Console.WriteLine($"Wrong order ({wrongOrder.GetProcessingSummary()}): " +
                               $"{wrongOrder.Process(rawFileBytes).Length} bytes");
            Console.WriteLine($"Right order ({rightOrder.GetProcessingSummary()}): " +
                               $"{rightOrder.Process(rawFileBytes).Length} bytes");
        }
    }
}

// ============================================================================
// LIKELY INTERVIEW FOLLOW-UPS (for your own reference while studying)
// ============================================================================
//
// Q: What if processors must run in a specific order and getting it wrong
//    is error-prone?
// A: Introduce a builder (e.g. PipelineBuilder) that enforces valid ordering,
//    or consider Chain of Responsibility if steps need to conditionally
//    short-circuit rather than always wrap.
//
// Q: How would you unit test this?
// A: Each decorator only depends on IFileProcessor, so you can mock the
//    wrapped processor and verify only the decorator's own added behavior
//    in isolation.
//
// Q: Why not just use a middleware/pipeline pattern instead?
// A: ASP.NET Core's middleware pipeline (Func<RequestDelegate, RequestDelegate>)
//    is conceptually the same idea as Decorator — function composition
//    instead of object wrapping, same underlying principle.
// ============================================================================