using AutoMapper;
using IMS.Models;
using IMS.ViewModels;

namespace IMS.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            // Entity -> ViewModel
            CreateMap<DeliveryChallan, DeliveryChallanViewModel>()
                .ForMember(dest => dest.ChallanNumber, opt => opt.MapFrom(src => src.ChallanNo))
                .ForMember(dest => dest.ReceiverPhone, opt => opt.MapFrom(src => src.ReceiverMobile))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<DeliveryChallanItem, DeliveryItemViewModel>()
                .ForMember(dest => dest.Particulars, opt => opt.MapFrom(src => src.Particular));

            // ViewModel -> Entity
            CreateMap<DeliveryChallanViewModel, DeliveryChallan>()
                .ForMember(dest => dest.ChallanNo, opt => opt.MapFrom(src => src.ChallanNumber))
                .ForMember(dest => dest.ReceiverMobile, opt => opt.MapFrom(src => src.ReceiverPhone))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<DeliveryItemViewModel, DeliveryChallanItem>()
                .ForMember(dest => dest.Particular, opt => opt.MapFrom(src => src.Particulars));
        }
    }
}
