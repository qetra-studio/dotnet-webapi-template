using JetBrains.Annotations;
using static JetBrains.Annotations.ImplicitUseKindFlags;
using static JetBrains.Annotations.ImplicitUseTargetFlags;

namespace RichWebApi.Models;

/// <summary>
/// A metadata token: anything with this interface is considered as something coming from outer world.
/// </summary>
[UsedImplicitly(Assign | InstantiatedNoFixedConstructorSignature, WithInheritors | WithMembers)]
public interface IInbound;