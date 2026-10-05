#import <UIKit/UIKit.h>

// Tests native renderer formats, not the full MAUI handler pipeline.
int main(void) {
    @autoreleasepool {
        for (int mode = 0; mode < 3; mode++) {
            UIGraphicsImageRenderer *renderer = [[UIGraphicsImageRenderer alloc] initWithSize:CGSizeMake(100, 100)];
            UIImage *image = [renderer imageWithActions:^(UIGraphicsImageRendererContext *context) {
                UIColor *color = mode == 0 ? UIColor.whiteColor : mode == 1 ? UIColor.redColor : [UIColor colorWithDisplayP3Red:1 green:0 blue:0 alpha:1];
                [color setFill];
                [context fillRect:CGRectMake(0, 0, 100, 100)];
            }];
            CGImageRef cg = image.CGImage;
            CFDataRef data = CGDataProviderCopyData(CGImageGetDataProvider(cg));
            const UInt8 *bytes = CFDataGetBytePtr(data);
            printf("pixel bytes: %02x %02x %02x %02x\n", bytes[0], bytes[1], bytes[2], bytes[3]);
            CFRelease(data);
            NSString *colorSpace = CFBridgingRelease(CGColorSpaceCopyName(CGImageGetColorSpace(cg)));
            printf("mode=%d width=%zu bpp=%zu bpc=%zu rowBytes=%zu bitmapInfo=%u colorSpace=%s\n", mode, CGImageGetWidth(cg), CGImageGetBitsPerPixel(cg), CGImageGetBitsPerComponent(cg), CGImageGetBytesPerRow(cg), CGImageGetBitmapInfo(cg), colorSpace.UTF8String);
        }
    }
    return 0;
}
