using Dav1dDotnet.Dav1d.Definitions;

namespace Dav1dDotnet.Decoder
{
    public readonly struct Av1Frame
    {
        public readonly Dav1dPicture Picture;

        public Av1Frame(Dav1dPicture picture)
        {
            Picture = picture;
        }
    }
}