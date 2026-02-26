using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    /// <summary>
    /// 管理员个人信息页面ViewModel
    /// </summary>
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IAdminProfileApi _adminProfileApi;
        private readonly IFileApi _fileApi;

        [ObservableProperty]
        private AdminProfile? _profile;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // 编辑表单字段
        [ObservableProperty]
        private string _nickname = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _avatar = string.Empty;

        // 密码修改字段
        [ObservableProperty]
        private string _oldPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ProfileViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _adminProfileApi = app.ApiServiceFactory.CreateAdminProfileApi();
            _fileApi = app.ApiServiceFactory.CreateFileApi();
        }

        /// <summary>
        /// 加载管理员信息
        /// </summary>
        public async Task LoadProfileAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            try
            {
                var response = await _adminProfileApi.GetProfileAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Profile = response.Data;
                    // 初始化编辑表单
                    Nickname = response.Data.Nickname ?? string.Empty;
                    Email = response.Data.Email ?? string.Empty;
                    Avatar = response.Data.Avatar ?? string.Empty;
                }
                else
                {
                    ErrorMessage = response.Message ?? "加载管理员信息失败";
                    OnPropertyChanged(nameof(HasError));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载管理员信息失败: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存管理员信息
        /// </summary>
        public async Task<(bool Success, string Message)> SaveProfileAsync()
        {
            IsSaving = true;

            try
            {
                var request = new AdminProfileUpdateRequest
                {
                    Nickname = string.IsNullOrWhiteSpace(Nickname) ? null : Nickname.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Avatar = string.IsNullOrWhiteSpace(Avatar) ? null : Avatar.Trim()
                };

                var response = await _adminProfileApi.UpdateProfileAsync(request);

                if (response.Code == 200 && response.Data != null)
                {
                    Profile = response.Data;
                    return (true, "信息更新成功");
                }
                else
                {
                    return (false, response.Message ?? "更新失败");
                }
            }
            catch (Exception ex)
            {
                return (false, $"更新失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync()
        {
            // 前端验证
            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                return (false, "请输入旧密码");
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                return (false, "请输入新密码");
            }

            if (NewPassword.Length < 8)
            {
                return (false, "新密码长度至少8位");
            }

            if (NewPassword != ConfirmPassword)
            {
                return (false, "新密码与确认密码不一致");
            }

            IsSaving = true;

            try
            {
                var request = new PasswordChangeRequest
                {
                    OldPassword = OldPassword,
                    NewPassword = NewPassword,
                    ConfirmPassword = ConfirmPassword
                };

                var response = await _adminProfileApi.ChangePasswordAsync(request);

                if (response.Code == 200)
                {
                    // 清空密码字段
                    OldPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                    return (true, "密码修改成功");
                }
                else
                {
                    return (false, response.Message ?? "密码修改失败");
                }
            }
            catch (Exception ex)
            {
                return (false, $"密码修改失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 上传头像（使用与评论/留言相同的接口）
        /// </summary>
        public async Task<(bool Success, string Message)> UploadAvatarAsync(Windows.Storage.StorageFile file)
        {
            try
            {
                using var stream = await file.OpenStreamForReadAsync();
                var streamPart = new Refit.StreamPart(stream, file.Name, file.ContentType);
                var response = await _fileApi.UploadAvatarAsync(streamPart);

                if (response.Code == 200 && response.Data != null)
                {
                    // 获取返回的URL
                    if (response.Data.TryGetValue("url", out var avatarUrl))
                    {
                        Avatar = avatarUrl;
                        return (true, "头像上传成功");
                    }
                    return (false, "上传成功但未获取到URL");
                }
                else
                {
                    return (false, response.Message ?? "头像上传失败");
                }
            }
            catch (Exception ex)
            {
                return (false, $"头像上传失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置编辑表单
        /// </summary>
        public void ResetForm()
        {
            if (Profile != null)
            {
                Nickname = Profile.Nickname ?? string.Empty;
                Email = Profile.Email ?? string.Empty;
                Avatar = Profile.Avatar ?? string.Empty;
            }

            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}
