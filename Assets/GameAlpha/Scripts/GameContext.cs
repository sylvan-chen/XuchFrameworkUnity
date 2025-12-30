// using System.Collections.Generic;
// using XuchFramework.Core.Utils;
//
// namespace GamePlay
// {
//     public class GameContext : Singleton<GameContext>
//     {
//         public PlayerData Player;
//         public PetData[] Pets = new PetData[1000];
//
//         #region Pet
//
//         private readonly Dictionary<int, int> _petIdToIndex = new(1000);
//         private int _petCount = 0;
//
//         public void AddPet(PetData pet)
//         {
//             if (_petCount >= Pets.Length)
//             {
//                 System.Array.Resize(ref Pets, Pets.Length * 2);
//             }
//
//             Pets[_petCount] = pet;
//             _petIdToIndex[pet.Id] = _petCount;
//             _petCount++;
//         }
//
//         public ref PetData GetPetById(int petId)
//         {
//             return ref Pets[_petIdToIndex[petId]];
//         }
//
//         public void RemovePet(int id)
//         {
//             // Swap Back
//
//             if (!_petIdToIndex.TryGetValue(id, out int indexToRemove))
//                 return;
//
//             if (indexToRemove != _petCount - 1)
//             {
//                 // Move the last element to the vacated position
//                 Pets[indexToRemove] = Pets[_petCount - 1];
//                 // Update the index of the last pet
//                 _petIdToIndex[Pets[indexToRemove].Id] = indexToRemove;
//             }
//
//             _petIdToIndex.Remove(id);
//             _petCount--;
//
//             ref PetData removed = ref Pets[_petCount];
//             removed.Clear();
//         }
//
//         #endregion
//     }
// }

