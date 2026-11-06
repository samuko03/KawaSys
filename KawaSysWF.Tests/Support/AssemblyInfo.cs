using Xunit;

// Todos los escenarios comparten DBHelper (estático): se ejecutan de a uno.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
