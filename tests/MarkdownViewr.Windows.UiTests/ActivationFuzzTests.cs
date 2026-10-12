/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Fuzzes the real generated activation schema and path validator without creating a window, pipe, process or profile.</summary>
[Trait("Category", "Fuzz")]
public sealed class ActivationFuzzTests
{
   /// <summary>Only bounded absolute literal paths and the known schema can survive mutated JSON; no command/property can expand its capability.</summary>
   [Theory]
   [InlineData(22023)]
   [InlineData(99173)]
   public void Activation_MutatedJson_RejectsUnboundedOrCommandPayloads(int seed)
   {
      string? configured = Environment.GetEnvironmentVariable("K22_FUZZ_ITERATIONS");
      int iterations = 500;
      if(configured is not null && (!int.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations is < 1 or > 100000))
      {
         throw new InvalidOperationException("K22_FUZZ_ITERATIONS must be between1 and100000.");
      }
      var random = new Random(seed);
      for(int iteration = 0; iteration < iterations; iteration++)
      {
         string input = Generate(random);
         try
         {
            ActivationRequest? request;
            try
            {
               request = JsonSerializer.Deserialize(input, ActivationJsonContext.Default.ActivationRequest);
               if(request is null)
               {
                  continue;
               }
               request.Validate();
            }
            catch(Exception exception) when(exception is JsonException or InvalidDataException)
            {
               continue;
            }
            using var document = JsonDocument.Parse(input);
            Assert.All(document.RootElement.EnumerateObject(), property => Assert.Contains(property.Name, new[] { "Paths", "HasInvalidPath" }, StringComparer.Ordinal));
            Assert.InRange(request.Paths.Length, 0, 128);
            Assert.All(request.Paths, path =>
            {
               Assert.InRange(path.Length, 1, 32767);
               Assert.True(Path.IsPathFullyQualified(path));
               Assert.DoesNotContain(path, char.IsControl);
            });
            Assert.Equal(request.Paths, JsonSerializer.Deserialize(JsonSerializer.Serialize(request, ActivationJsonContext.Default.ActivationRequest), ActivationJsonContext.Default.ActivationRequest)!.Paths);
         }
         catch(Exception exception)
         {
            throw new InvalidOperationException(FormattableString.Invariant($"Target=activation; Seed={seed}; Iteration={iteration}; Input={JsonSerializer.Serialize(input)}"), exception);
         }
      }
   }



   /// <summary>Generated envelopes include foreign command fields, relative/control-bearing paths, excessive lists and malformed syntax.</summary>
   private static string Generate(Random random)
   {
      string[] corpus = ["C:\\k22-fuzz\\notes & café $HOME.md", "relative.md", "", "C:\\k22-fuzz\\line\nbreak.md", "\\\\synthetic.invalid\\share\\notes.md", "C:\\k22-fuzz\\literal \" quote.md"];
      var paths = new JsonArray();
      int count = random.Next(0, 145);
      bool validPaths = random.Next(3) == 0;
      for(int index = 0; index < count; index++)
      {
         paths.Add(validPaths ? corpus[0] : corpus[random.Next(corpus.Length)]);
      }
      var value = new JsonObject { ["Paths"] = paths, ["HasInvalidPath"] = random.Next(2) == 1 };
      if(random.Next(4) == 0)
      {
         value["command"] = "execute-synthetic-content";
      }
      string input = value.ToJsonString();
      return random.Next(6) == 0 ? input.Remove(random.Next(input.Length), 1) : input;
   }
}
