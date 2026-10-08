using Kompas6API5;
using Kompas6Constants;
using Kompas6Constants3D;

namespace Kompas3DShaftLibrary
{
    public class ShaftModeler : IDisposable
    {
        private readonly KompasObject? kompasObject;
        private ksDocument3D? doc3D;
        private bool disposed = false;

        public ShaftModeler()
        {
            try
            {
                this.kompasObject = (KompasObject?)Activator.CreateInstance(Type.GetTypeFromProgID("KOMPAS.Application.5"));
                if (this.kompasObject != null)
                {
                    this.kompasObject.Visible = true;
                }
                else
                {
                    throw new InvalidOperationException("Failed to create Kompas-3D application instance.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error initializing Kompas-3D", ex);
            }
        }

        ~ShaftModeler()
        {
            this.Dispose(false);
        }

        public void CreateNewDocument()
        {
            this.doc3D = kompasObject?.Document3D() as ksDocument3D;
            if (this.doc3D == null)
            {
                throw new InvalidOperationException("Failed to create new 3D document.");
            }
            this.doc3D?.Create(false, true);
        }

        public void CreateShaft(double length, double diameter)
        {
            if (this.doc3D == null)
            {
                throw new InvalidOperationException("No active 3D document.");
            }
            ksPart? part = (ksPart?)this.doc3D.GetPart((short)Part_Type.pTop_Part);
            if (part == null)
            {
                throw new InvalidOperationException("Failed to get top part.");
            }
            double flangeThickness = diameter * 0.2;
            double flangeDiameter = diameter * 1.5;
            double keyWidth = diameter * 0.25;
            double keyDepth = diameter * 0.125;
            double keyLength = length * 0.3;
            CreateCylindricalBody(part, length, diameter);
            CreateFlange(part, flangeThickness, flangeDiameter);
            CreateKeyway(part, keyWidth, keyDepth, keyLength);
        }

        private static void CreateCylindricalBody(ksPart part, double length, double diameter)
        {
            ksEntity? sketch = CreateSketch(part, (short)Obj3dType.o3d_planeXOY);
            ksSketchDefinition? sketchDef = (ksSketchDefinition?)sketch.GetDefinition();
            ksDocument2D? sketchEdit = (ksDocument2D?)sketchDef?.BeginEdit();
            sketchEdit?.ksCircle(0, 0, diameter / 2, 1);
            sketchDef?.EndEdit();
            CreateExtrusion(part, sketch, length);
        }

        private static void CreateFlange(ksPart part, double thickness, double diameter)
        {
            ksEntity? sketch = CreateSketch(part, (short)Obj3dType.o3d_planeXOY);
            ksSketchDefinition? sketchDef = (ksSketchDefinition?)sketch.GetDefinition();
            ksDocument2D? sketchEdit = (ksDocument2D?)sketchDef?.BeginEdit();
            sketchEdit?.ksCircle(0, 0, diameter / 2, 1);
            sketchDef?.EndEdit();
            CreateExtrusion(part, sketch, thickness);
        }

        private void CreateKeyway(ksPart part, double width, double depth, double length)
        {
            ksEntity? sketch = CreateSketch(part, (short)Obj3dType.o3d_planeXOZ);
            ksSketchDefinition? sketchDef = (ksSketchDefinition?)sketch.GetDefinition();
            ksDocument2D? sketchEdit = (ksDocument2D?)sketchDef?.BeginEdit();
            ksRectangleParam? rectangleParams = null;
            if (this.kompasObject != null)
            {
                rectangleParams = (ksRectangleParam)this.kompasObject.GetParamStruct((short)StructType2DEnum.ko_RectangleParam);
            }
            if (rectangleParams != null)
            {
                rectangleParams.x = 0;
                rectangleParams.y = 0;
                rectangleParams.width = width;
                rectangleParams.height = -depth;
                rectangleParams.style = 1;
                sketchEdit?.ksRectangle(rectangleParams);
            }
            sketchDef?.EndEdit();
            CreateCutExtrusion(part, sketch, length);
        }

        private static ksEntity CreateSketch(ksPart part, short planeType)
        {
            ksEntity? sketch = (ksEntity?)part.NewEntity((short)Obj3dType.o3d_sketch);
            if (sketch == null)
            {
                throw new InvalidOperationException("Failed to create sketch.");
            }
            ksSketchDefinition? sketchDef = (ksSketchDefinition?)sketch.GetDefinition();
            if (sketchDef == null)
            {
                throw new InvalidOperationException("Failed to get sketch definition.");
            }
            sketchDef?.SetPlane(part.GetDefaultEntity(planeType));
            sketch.Create();
            return sketch;
        }

        private static void CreateExtrusion(ksPart part, ksEntity sketch, double length)
        {
            ksEntity? extrude = (ksEntity?)part.NewEntity((short)Obj3dType.o3d_baseExtrusion);
            if (extrude == null)
            {
                throw new InvalidOperationException("Failed to create extrusion entity.");
            }
            ksBaseExtrusionDefinition? extrudeDef = (ksBaseExtrusionDefinition?)extrude.GetDefinition();
            if (extrudeDef == null)
            {
                throw new InvalidOperationException("Failed to get extrusion definition.");
            }
            extrudeDef.directionType = (short)Direction_Type.dtNormal;
            extrudeDef.SetSideParam(true, (short)End_Type.etBlind, length);
            extrudeDef.SetSketch(sketch);
            extrude.Create();
        }

        private static void CreateCutExtrusion(ksPart part, ksEntity sketch, double length)
        {
            ksEntity? cut = (ksEntity?)part.NewEntity((short)Obj3dType.o3d_cutExtrusion);
            if (cut == null)
            {
                throw new InvalidOperationException("Failed to create cut extrusion entity.");
            }
            ksCutExtrusionDefinition? cutDef = (ksCutExtrusionDefinition?)cut.GetDefinition();
            if (cutDef == null)
            {
                throw new InvalidOperationException("Failed to get cut extrusion definition.");
            }
            cutDef.directionType = (short)Direction_Type.dtNormal;
            cutDef.SetSideParam(true, (short)End_Type.etBlind, length);
            cutDef.SetSketch(sketch);
            cut.Create();
        }

        public void SaveDocument(string filePath)
        {
            this.doc3D?.SaveAs(filePath);
        }

        public void CloseDocument()
        {
            this.doc3D?.close();
            this.doc3D = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.CloseDocument();
                    if (kompasObject != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(kompasObject);
                    }
                }
                disposed = true;
            }
        }
    }
}
