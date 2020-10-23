using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userID}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private Cloudinary _cloudinary;

        public IDatingRepository _repository { get; }
        public IMapper _mapper { get; }
        public IOptions<CloudinarySettings> _cloudinaryConfig { get; }
        public PhotosController(IDatingRepository repository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repository = repository;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }
        [HttpGet("/{id}", Name ="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo= await _repository.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userID, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await _repository.GetUser(userID);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            photoForCreationDto.Url=uploadResult.Url.ToString();
            photoForCreationDto.PublicId=uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;
            userFromRepo.Photos.Add(photo);


            if(await _repository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute(nameof(GetPhoto), new { id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userID, int id)
        {
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var user = await _repository.GetUser(userID);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();
            
            var photoFromRepo = await _repository.GetPhoto(id);

            if(photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");
            
            var currentMainPhoto = await _repository.GetMainPhotoForUser(userID);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if(await _repository.SaveAll())
                return NoContent();
            
            return BadRequest("Could not set photo to main!");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userID, int id)
        {
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var user = await _repository.GetUser(userID);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();
            
            var photoFromRepo = await _repository.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");
            
            //If it has publicID it will delete from cloudinary
            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var deletionResult =  _cloudinary.Destroy(deleteParams);

                if (deletionResult.Result == "ok") {
                    _repository.Delete(photoFromRepo);
                }
            }
            //If it does not have publicID it will delete normal picture
            if (photoFromRepo.PublicId == null)
            {
                _repository.Delete(photoFromRepo);
            }
            
            if (await _repository.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}