using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
#if NET35||PCL
using System.Linq;
#endif
using System.Reflection;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{
    /// <summary>
    /// Registry class for all known <see cref="MapProjection"/>s.
    /// </summary>
    public class ProjectionsRegistry
    {
        private static readonly Dictionary<string, int> ConstructorRegistry = new Dictionary<string, int>();
#if NET45
        private static readonly Dictionary<string, TypeInfo> TypeRegistry = new Dictionary<string, TypeInfo>();
        private static readonly Dictionary<TypeInfo, int> ConstructorIndex = new Dictionary<TypeInfo, int>();
#else
        private static readonly Dictionary<string, Type> TypeRegistry = new Dictionary<string, Type>();
#endif
        private static readonly object RegistryLock = new object();

        /// <summary>
        /// Static constructor
        /// </summary>
        static ProjectionsRegistry()
        {
#if NET45
            //ConstructorIndex.Add(typeof(ParameterInfo[]).GetTypeInfo(), 0);
            ConstructorIndex.Add(typeof(IEnumerable<ProjectionParameter>).GetTypeInfo(), 1);
            ConstructorIndex.Add(typeof(List<ProjectionParameter>).GetTypeInfo(), 2);
            ConstructorIndex.Add(typeof(IList<ProjectionParameter>).GetTypeInfo(), 3);
            ConstructorIndex.Add(typeof(ICollection<ProjectionParameter>).GetTypeInfo(), 4);
#endif
            Register("mercator", typeof(Mercator));
            Register("mercator_1sp", typeof (Mercator));
            Register("mercator_2sp", typeof (Mercator));
            Register("pseudo-mercator", typeof(PseudoMercator));
            Register("popular_visualisation pseudo-mercator", typeof(PseudoMercator));
            Register("google_mercator", typeof(PseudoMercator));
			
            Register("transverse_mercator", typeof(TransverseMercator));

            Register("albers", typeof(AlbersProjection));
			Register("albers_conic_equal_area", typeof(AlbersProjection));

			Register("krovak", typeof(KrovakProjection));

			Register("polyconic", typeof(PolyconicProjection));
			
            Register("lambert_conformal_conic", typeof(LambertConformalConic2SP));
			Register("lambert_conformal_conic_2sp", typeof(LambertConformalConic2SP));
			Register("lambert_conic_conformal_(2sp)", typeof(LambertConformalConic2SP));

            Register("cassini_soldner", typeof(CassiniSoldnerProjection));
            Register("hotine_oblique_mercator", typeof(HotineObliqueMercatorProjection));
            Register("oblique_mercator", typeof(ObliqueMercatorProjection));
            Register("oblique_stereographic", typeof(ObliqueStereographicProjection));
        }

#if !NET45 
        /// <summary>
        /// Method to register a new projection type
        /// </summary>
        /// <param name="name">The name of the projection</param>
        /// <param name="type">The type of the projection</param>
        public static void Register(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IMathTransform).IsAssignableFrom(type))
                throw new ArgumentException("The provided type does not implement 'GeoAPI.CoordinateSystems.Transformations.IMathTransform'!", "type");

            var ci = CheckConstructor(type);
            if (ci == 0)
                throw new ArgumentException("The provided type is lacking a suitable constructor", "type");

            var key = name.ToLowerInvariant().Replace(' ', '_');
            lock (RegistryLock)
            {
                if (TypeRegistry.ContainsKey(key))
                {
                    var rt = TypeRegistry[key];
                    if (ReferenceEquals(type, rt))
                        return;
                    throw new ArgumentException("A different projection type has been registered with this name", "name");
                }

                TypeRegistry.Add(key, type);
                ConstructorRegistry.Add(key, ci);
            }
        }

        private static int CheckConstructor(Type type)
        {
            var c = type.GetConstructor(new[] { typeof(IEnumerable<ProjectionParameter>) });
            if (c != null)
                return 1;

            c = type.GetConstructor(new[] { typeof(List<ProjectionParameter>) });
            if (c != null)
                return 2;

            c = type.GetConstructor(new[] { typeof(IList<ProjectionParameter>) });
            if (c != null)
                return 3;
            
            c = type.GetConstructor(new[] { typeof(ICollection<ProjectionParameter>) });
            return c != null ? 4 : 0;
        }
#else
        /// <summary>
        /// Method to register a new projection type
        /// </summary>
        /// <param name="name">The name of the projection</param>
        /// <param name="type">The type of the projection</param>
        public static void Register(string name, Type type)
        {
            var ti = type.GetTypeInfo();
            Register(name, ti);
        }

        /// <summary>
        /// Method to register a new Map
        /// </summary>
        /// <param name="name"></param>
        /// <param name="typeInfo"></param>
        public static void Register(string name, TypeInfo typeInfo)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (typeInfo == null)
                throw new ArgumentNullException("type");

            if (!typeInfo.ImplementedInterfaces.Contains(typeof(IMathTransform)))
                throw new ArgumentException("The provided type does not implement 'GeoAPI.CoordinateSystems.Transformations.IMathTransform'!", "typeInfo");

            var ci = CheckConstructor(typeInfo);
            if (ci == 0)
                throw new ArgumentException("The provided type is lacking a suitable constructor", "typeInfo");

            var key = name.ToLowerInvariant().Replace(' ', '_');
            lock (RegistryLock)
            {
                if (TypeRegistry.ContainsKey(key))
                {
                    var rt = TypeRegistry[key];
                    if (ReferenceEquals(typeInfo, rt))
                        return;
                    throw new ArgumentException("A different projection type has been registered with this name", "name");
                }

                TypeRegistry.Add(key, typeInfo);
                ConstructorRegistry.Add(key, ci);
            }
        }

        private static int CheckConstructor(TypeInfo type)
        {
            var constructors = type.DeclaredConstructors;
            foreach (var ci in constructors)
            {
                var pi = ci.GetParameters();
                var ti = pi[0].ParameterType.GetTypeInfo();
                int index;
                if (ConstructorIndex.TryGetValue(ti, out index))
                    return index;
            }
            return 0;
        }
#endif

        internal static IMathTransform CreateProjection(string className, IEnumerable<ProjectionParameter> parameters)
        {
            var key = className.ToLowerInvariant().Replace(' ', '_');

#if !NET45 || PCL40
            Type projectionTypeInfo;
#else
            TypeInfo projectionTypeInfo;
#endif
            int ci;


            lock (RegistryLock)
            {
                if (!TypeRegistry.TryGetValue(key, out projectionTypeInfo))
                    throw new NotSupportedException(String.Format("Projection {0} is not supported.", className));
                ci = ConstructorRegistry[key];
            }

#if !NET45 || PCL40
            var projectionType = projectionTypeInfo;
#else
            var projectionType = projectionTypeInfo.AsType();
#endif
            switch (ci)
            {
                case 1:
                    return (IMathTransform) Activator.CreateInstance(projectionType, parameters);
                case 2:
                    var l = parameters as List<ProjectionParameter> ?? new List<ProjectionParameter>(parameters);
                    return (IMathTransform)Activator.CreateInstance(projectionType, l);
                case 3:
                    var il = parameters as IList<ProjectionParameter> ?? new List<ProjectionParameter>(parameters);
                    return (IMathTransform)Activator.CreateInstance(projectionType, il);
                case 4:
                    var ic = parameters as ICollection<ProjectionParameter> ?? new List<ProjectionParameter>(parameters);
                    return (IMathTransform)Activator.CreateInstance(projectionType, ic);
            }

            throw new NotSupportedException("Should never reach here!");
        }
    }
}