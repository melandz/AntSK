﻿using AntDesign;
using Microsoft.AspNetCore.Components;
using AntSK.Domain.Repositories;
using AntSK.Models;
using System.IO;

namespace AntSK.Pages.AppPage
{
    public partial class AddApp
    {
        [Inject]
        protected IApps_Repositories _apps_Repositories { get; set; }

        [Inject]
        protected IKmss_Repositories _kmss_Repositories { get; set; }
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        private Apps _appModel = new Apps() ;

        IEnumerable <string>  kmsIds;

        private List<Kmss> _kmsList = new List<Kmss>();


        private string _errorMsg { get; set; }

        private readonly FormItemLayout _formItemLayout = new FormItemLayout
        {
            LabelCol = new ColLayoutParam
            {
                Xs = new EmbeddedProperty { Span = 24 },
                Sm = new EmbeddedProperty { Span = 7 },
            },

            WrapperCol = new ColLayoutParam
            {
                Xs = new EmbeddedProperty { Span = 24 },
                Sm = new EmbeddedProperty { Span = 12 },
                Md = new EmbeddedProperty { Span = 10 },
            }
        };
        private readonly FormItemLayout _submitFormLayout = new FormItemLayout
        {
            WrapperCol = new ColLayoutParam
            {
                Xs = new EmbeddedProperty { Span = 24, Offset = 0 },
                Sm = new EmbeddedProperty { Span = 10, Offset = 7 },
            }
        };
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _kmsList = _kmss_Repositories.GetList();
        }
        private void HandleSubmit()
        {
            _appModel.Id = Guid.NewGuid().ToString();
            if (_apps_Repositories.IsAny(p => p.Name == _appModel.Name))
            {
                _errorMsg = "名称已存在！";
                return;
            }

            if (kmsIds.Count() > 0)
            {
                _appModel.KmsIdList = string.Join(",", kmsIds);
            }

            _apps_Repositories.Insert(_appModel);

            //NavigationManager.NavigateTo($"/app/detail/{_appModel.Id}");
            NavigationManager.NavigateTo($"/applist");
        }

        private void OnSelectedItemChangedHandler(string value)
        {
            Console.WriteLine($"selected: ${value}");
        }

    }

}
