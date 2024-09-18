using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.Emit;
using System.Data.SqlTypes;
using System.Xml;
using System.Net.Mime;
using System.Windows.Controls;


namespace WpfApp1
{

    public sealed partial class Tools
    {
        public static int maxSurfaces = 0;
        private static async Task<string> getPrusaModelInfo(string rawStl)
        {
            string verticeTriangles = importPrusaBinary(rawStl);
            // return package;
            return verticeTriangles;
        }

        private static string importPrusaBinary(string fileName)
        {

            int vectorCount = 0;
            Dictionary<Vector3, int> Uniques = new Dictionary<Vector3, int>();

            XmlDocument docElem = new XmlDocument();
            XmlDeclaration xmldecl = docElem.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlElement rootElem = docElem.DocumentElement;
            docElem.InsertBefore(xmldecl, rootElem);
            XmlElement modelElem = docElem.CreateElement("model", "http://schemas.microsoft.com/3dmanufacturing/core/2015/02");
            modelElem.SetAttribute("unit", "millimeter");
            modelElem.SetAttribute("xmlns:slic3rpe", "http://schemas.slic3r.org/3mf/2017/06");
            modelElem.SetAttribute("xml:lang", "en-US");
            docElem.AppendChild(modelElem);

            modelElem.AppendChild(createMeta(docElem, "slic3rpe:Version3mf", "1"));
            modelElem.AppendChild(createMeta(docElem, "Title", "1"));
            modelElem.AppendChild(createMeta(docElem, "Designer", ""));
            modelElem.AppendChild(createMeta(docElem, "Description", "PrusaFile"));
            modelElem.AppendChild(createMeta(docElem, "Copyright", ""));
            modelElem.AppendChild(createMeta(docElem, "LicenseTerms", ""));
            modelElem.AppendChild(createMeta(docElem, "Rating", ""));
            modelElem.AppendChild(createMeta(docElem, "CreationDate", "2024-09-17"));
            modelElem.AppendChild(createMeta(docElem, "ModificationDate", "2024-09-17"));
            modelElem.AppendChild(createMeta(docElem, "Application", "PrusaSlicer-2.5.2+win64"));

            XmlElement resourceElem = docElem.CreateElement("resources", docElem.DocumentElement.NamespaceURI);
            modelElem.AppendChild(resourceElem);

            XmlElement objectElem = docElem.CreateElement("object");
            objectElem.SetAttribute("id", "1");
            objectElem.SetAttribute("type", "model");
            objectElem.RemoveAttribute("xmlns");
            resourceElem.AppendChild(objectElem);

            XmlElement meshElem = docElem.CreateElement("mesh", docElem.DocumentElement.NamespaceURI);
            meshElem.RemoveAttribute("xmlns");
            objectElem.AppendChild(meshElem);
            XmlElement verticesElem = docElem.CreateElement("vertices", docElem.DocumentElement.NamespaceURI);
            verticesElem.RemoveAttribute("xmlns");
            meshElem.AppendChild(verticesElem);
            XmlElement trianglesElem = docElem.CreateElement("triangles", docElem.DocumentElement.NamespaceURI);
            trianglesElem.RemoveAttribute("xmlns");
            meshElem.AppendChild(trianglesElem);


            XmlElement buildElem = docElem.CreateElement("build", docElem.DocumentElement.NamespaceURI);
            buildElem.RemoveAttribute("xmlns");
            modelElem.AppendChild(buildElem);
            XmlElement itemElem = docElem.CreateElement("item", docElem.DocumentElement.NamespaceURI);
            itemElem.SetAttribute("objectid", "1");
            itemElem.SetAttribute("transform", "1 0 0 0 1 0 0 0 1 110 110 1.07999992");
            itemElem.SetAttribute("printable", "1");
            itemElem.RemoveAttribute("xmlns");
            buildElem.AppendChild(itemElem);

            try
            {
                Object locker = new Object();
                lock (locker)
                {
                    using (BinaryReader br = new BinaryReader(File.Open(fileName, FileMode.Open)))
                    {
                        byte[] header = br.ReadBytes(80);
                        byte[] length = br.ReadBytes(4);
                        int numberOfSurfaces = BitConverter.ToInt32(length, 0);
                        string headerInfo = Encoding.UTF8.GetString(header, 0, header.Length).Trim();
                        System.Diagnostics.Debug.WriteLine(String.Format("Number of faces:{0}", numberOfSurfaces));
                        maxSurfaces = numberOfSurfaces - 1;
                        byte[] full = ReadAllBytes(br.BaseStream);
                        byte[] block = new byte[50];
                        int surfCount = 0;
                        while (surfCount < numberOfSurfaces)
                        {
                            byte[] xComp = new byte[4];
                            byte[] yComp = new byte[4];
                            byte[] zComp = new byte[4];
                            int offset = surfCount * 50;
                            List<int> MyFace = new List<int>();
                            for (int i = 1; i < 4; i++)
                            {
                                for (int k = 0; k < 12; k++)
                                {
                                    int index = (k + i * 12) + offset;

                                    if (k < 4)
                                    {
                                        xComp[k] = full[index];
                                    }
                                    else if (k < 8)
                                    {
                                        yComp[k - 4] = full[index];
                                    }
                                    else
                                    {
                                        zComp[k - 8] = full[index];
                                    }
                                }
                                Vector3 vert = new Vector3();
                                vert.X = BitConverter.ToSingle(xComp, 0);
                                vert.Y = BitConverter.ToSingle(yComp, 0);
                                vert.Z = BitConverter.ToSingle(zComp, 0);

                                if (!Uniques.ContainsKey(vert))
                                {
                                    XmlElement vertexElem = docElem.CreateElement("vertex");
                                    vertexElem.SetAttribute("x", vert.X.ToString());
                                    vertexElem.SetAttribute("y", vert.Y.ToString());
                                    vertexElem.SetAttribute("z", vert.Z.ToString());
                                    vertexElem.RemoveAttribute("xmlns");
                                    verticesElem.AppendChild(vertexElem);
                                    Uniques.Add(vert, vectorCount);
                                    vectorCount++;
                                }

                                MyFace.Add(Uniques[vert]);
                                if (i == 3)
                                {
                                    XmlElement triangleElem = docElem.CreateElement("triangle");
                                    var v1 = MyFace[0];
                                    var v2 = MyFace[1];
                                    var v3 = MyFace[2];
                                    triangleElem.SetAttribute("v1", v1.ToString());
                                    triangleElem.SetAttribute("v2", v2.ToString());
                                    triangleElem.SetAttribute("v3", v3.ToString());
                                    triangleElem.RemoveAttribute("xmlns");
                                    trianglesElem.AppendChild(triangleElem);
                                }
                            }
                            surfCount++;
                        }
                    }


                    docElem.PreserveWhitespace = true;
                    return docElem.OuterXml;
                }

            }
            catch (Exception e)  // This is too general to be the only catch statement.
            {
                System.Diagnostics.Debug.WriteLine("The file could not be read:");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return "";

            }
        }

        private static async Task<string> getPrusaCuttingInfo(JObject HfpData, JArray Colors)
        {
            var layer_height = HfpData["layer_height"];
            var base_layer_height = HfpData["base_layer_height"];

            string outputCut = "";
            int extruderCount = 1;
            var swapCount = 0;
            var maxSwaps = Colors.Count-1;
            foreach (JValue value in HfpData["slider_values"])
            {
                if (swapCount < maxSwaps) { // weird color swap storage in hueforge
                    var zHeight = (value.Value<double>()) * layer_height.Value<double>() + base_layer_height.Value<double>();
                    var colorhere = Colors[extruderCount];
                    outputCut += "<code print_z=\"" + zHeight + "\" type=\"0\" extruder=\"1\" color=\"" + colorhere.Value<string>() + "\" extra=\"\" gcode=\"M600\"/>\r\n";
                    extruderCount++;
                }
                swapCount++;
            }

            var cutXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<custom_gcodes_per_print_z>\r\n" +
            outputCut +
            "<mode value=\"SingleExtruder\"/>\r\n" +
            "</custom_gcodes_per_print_z>\r\n";

            return cutXml;
        }

        public static async void CreatePrusaPackage(string inputFile)
        {
            JObject HfpData = JObject.Parse(File.ReadAllText(inputFile));
            string Folder = inputFile;
            if (inputFile.LastIndexOf("\\") > 0)
                Folder = inputFile.Substring(0, inputFile.LastIndexOf("\\") + 1);
            else if (inputFile.LastIndexOf("/") > 0)
                Folder = inputFile.Substring(0, inputFile.LastIndexOf("/") + 1);
            string stlOrig = HfpData["stl"].Value<string>();
            if (stlOrig.LastIndexOf("\\") > 0)
                stlOrig = stlOrig.Substring(stlOrig.LastIndexOf("\\") + 1);
            else if (stlOrig.LastIndexOf("/") > 0)
                stlOrig = stlOrig.Substring(stlOrig.LastIndexOf("/") + 1);
            string outputPath = inputFile.Replace("hfp", "3mf");
            var stlName = Folder + stlOrig;


            var globalRel = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Target=\"/3D/3dmodel.model\" Id=\"rel-1\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" />" +
            "</Relationships>";

            var stringMeshXml = await getPrusaModelInfo(stlName);

            var sliceInfoXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<config>" +
                " <object id=\"1\" instances_count=\"1\">" +
                "  <metadata type=\"object\" key=\"name\" value=\"" + stlOrig + "\"/>" +
                "  <volume firstid=\"0\" lastid=\"" + maxSurfaces + "\">" +
                "   <metadata type=\"volume\" key=\"name\" value=\"" + stlOrig + "\"/>" +
                "   <metadata type=\"volume\" key=\"volume_type\" value=\"ModelPart\"/>" +
                "   <metadata type=\"volume\" key=\"matrix\" value=\"1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1\"/>" +
                "   <metadata type=\"volume\" key=\"source_file\" value=\"" + stlOrig + "\"/>" +
                "   <metadata type=\"volume\" key=\"source_object_id\" value=\"0\"/>" +
                "   <metadata type=\"volume\" key=\"source_volume_id\" value=\"0\"/>" +
                "   <metadata type=\"volume\" key=\"source_offset_x\" value=\"0\"/>" +
                "   <metadata type=\"volume\" key=\"source_offset_y\" value=\"0\"/>" +
                "   <metadata type=\"volume\" key=\"source_offset_z\" value=\"1.0799999237060547\"/>" +
                "   <mesh edges_fixed=\"0\" degenerate_facets=\"0\" facets_removed=\"0\" facets_reversed=\"0\" backwards_edges=\"0\"/>" +
                "  </volume>" +
                " </object>" +
                "</config>";

            var layer_height = HfpData["layer_height"];
            var base_layer_height = HfpData["base_layer_height"];
            var projectData = "; generated by PrusaSlicer 2.5.2+win64 on 2024-09-17 at 20:38:18 UTC\r\n\r\n; avoid_crossing_perimeters = 0\r\n; avoid_crossing_perimeters_max_detour = 0\r\n; bed_custom_model = \r\n; bed_custom_texture = \r\n; bed_shape = 0x0,220x0,220x220,0x220\r\n; bed_temperature = 60\r\n; before_layer_gcode = \r\n; between_objects_gcode = \r\n; bottom_fill_pattern = monotonic\r\n; bottom_solid_layers = 3\r\n; bottom_solid_min_thickness = 0\r\n; bridge_acceleration = 0\r\n; bridge_angle = 0\r\n; bridge_fan_speed = 100\r\n; bridge_flow_ratio = 1\r\n; bridge_speed = 60\r\n; brim_separation = 0\r\n; brim_type = outer_only\r\n; brim_width = 0\r\n; clip_multipart_objects = 1\r\n; color_change_gcode = M600\r\n; colorprint_heights = \r\n; complete_objects = 0\r\n; cooling = 1\r\n; cooling_tube_length = 5\r\n; cooling_tube_retraction = 91.5\r\n; default_acceleration = 0\r\n; default_filament_profile = \r\n; default_print_profile = \r\n; deretract_speed = 0\r\n; disable_fan_first_layers = 3\r\n; dont_support_bridges = 1\r\n; draft_shield = disabled\r\n; duplicate_distance = 6\r\n; elefant_foot_compensation = 0\r\n; end_filament_gcode = \"; Filament-specific end gcode \\n;END gcode for filament\\n\"\r\n; end_gcode = M104 S0 ; turn off temperature\\nG28 X0  ; home X axis\\nM84     ; disable motors\\n\r\n; ensure_vertical_shell_thickness = 0\r\n; external_perimeter_acceleration = 0\r\n; external_perimeter_extrusion_width = 0.45\r\n; external_perimeter_speed = 50%\r\n; external_perimeters_first = 0\r\n; extra_loading_move = -2\r\n; extra_perimeters = 1\r\n; extruder_clearance_height = 20\r\n; extruder_clearance_radius = 20\r\n; extruder_colour = \"\"\r\n; extruder_offset = 0x0\r\n; extrusion_axis = E\r\n; extrusion_multiplier = 1\r\n; extrusion_width = 0.45\r\n; fan_always_on = 0\r\n; fan_below_layer_time = 60\r\n; filament_colour = {firstColor}\r\n; filament_cooling_final_speed = 3.4\r\n; filament_cooling_initial_speed = 2.2\r\n; filament_cooling_moves = 4\r\n; filament_cost = 0\r\n; filament_density = 0\r\n; filament_deretract_speed = nil\r\n; filament_diameter = 1.75\r\n; filament_load_time = 0\r\n; filament_loading_speed = 28\r\n; filament_loading_speed_start = 3\r\n; filament_max_volumetric_speed = 0\r\n; filament_minimal_purge_on_wipe_tower = 15\r\n; filament_notes = \"\"\r\n; filament_ramming_parameters = \"120 100 6.6 6.8 7.2 7.6 7.9 8.2 8.7 9.4 9.9 10.0| 0.05 6.6 0.45 6.8 0.95 7.8 1.45 8.3 1.95 9.7 2.45 10 2.95 7.6 3.45 7.6 3.95 7.6 4.45 7.6 4.95 7.6\"\r\n; filament_retract_before_travel = nil\r\n; filament_retract_before_wipe = nil\r\n; filament_retract_layer_change = nil\r\n; filament_retract_length = nil\r\n; filament_retract_lift = nil\r\n; filament_retract_lift_above = nil\r\n; filament_retract_lift_below = nil\r\n; filament_retract_restart_extra = nil\r\n; filament_retract_speed = nil\r\n; filament_settings_id = Ender3BLTouch\r\n; filament_soluble = 0\r\n; filament_spool_weight = 0\r\n; filament_toolchange_delay = 0\r\n; filament_type = PLA\r\n; filament_unload_time = 0\r\n; filament_unloading_speed = 90\r\n; filament_unloading_speed_start = 100\r\n; filament_vendor = (Unknown)\r\n; filament_wipe = nil\r\n; fill_angle = 45\r\n; fill_density = 20%\r\n; fill_pattern = stars\r\n; first_layer_acceleration = 0\r\n; first_layer_acceleration_over_raft = 0\r\n; first_layer_bed_temperature = 60\r\n; first_layer_extrusion_width = 0.42\r\n; first_layer_height = {first_layer_height}\r\n; first_layer_speed = 30\r\n; first_layer_speed_over_raft = 30\r\n; first_layer_temperature = 215\r\n; full_fan_speed_layer = 0\r\n; fuzzy_skin = none\r\n; fuzzy_skin_point_dist = 0.8\r\n; fuzzy_skin_thickness = 0.3\r\n; gap_fill_enabled = 1\r\n; gap_fill_speed = 20\r\n; gcode_comments = 0\r\n; gcode_flavor = marlin2\r\n; gcode_label_objects = 0\r\n; gcode_resolution = 0.0125\r\n; gcode_substitutions = \r\n; high_current_on_filament_swap = 0\r\n; host_type = octoprint\r\n; infill_acceleration = 0\r\n; infill_anchor = 600%\r\n; infill_anchor_max = 50\r\n; infill_every_layers = 1\r\n; infill_extruder = 1\r\n; infill_extrusion_width = 0.45\r\n; infill_first = 0\r\n; infill_only_where_needed = 0\r\n; infill_overlap = 25%\r\n; infill_speed = 80\r\n; interface_shells = 0\r\n; ironing = 0\r\n; ironing_flowrate = 15%\r\n; ironing_spacing = 0.1\r\n; ironing_speed = 15\r\n; ironing_type = top\r\n; layer_gcode = \r\n; layer_height = {layer_height}\r\n; machine_limits_usage = time_estimate_only\r\n; machine_max_acceleration_e = 10000,5000\r\n; machine_max_acceleration_extruding = 1500,1250\r\n; machine_max_acceleration_retracting = 1500,1250\r\n; machine_max_acceleration_travel = 1500,1250\r\n; machine_max_acceleration_x = 9000,1000\r\n; machine_max_acceleration_y = 9000,1000\r\n; machine_max_acceleration_z = 500,200\r\n; machine_max_feedrate_e = 120,120\r\n; machine_max_feedrate_x = 500,200\r\n; machine_max_feedrate_y = 500,200\r\n; machine_max_feedrate_z = 12,12\r\n; machine_max_jerk_e = 2.5,2.5\r\n; machine_max_jerk_x = 10,10\r\n; machine_max_jerk_y = 10,10\r\n; machine_max_jerk_z = 0.2,0.4\r\n; machine_min_extruding_rate = 0,0\r\n; machine_min_travel_rate = 0,0\r\n; max_fan_speed = 100\r\n; max_layer_height = 0\r\n; max_print_height = 200\r\n; max_print_speed = 80\r\n; max_volumetric_extrusion_rate_slope_negative = 0\r\n; max_volumetric_extrusion_rate_slope_positive = 0\r\n; max_volumetric_speed = 0\r\n; min_bead_width = 85%\r\n; min_fan_speed = 35\r\n; min_feature_size = 25%\r\n; min_layer_height = 0.07\r\n; min_print_speed = 10\r\n; min_skirt_length = 0\r\n; mmu_segmented_region_max_width = 0\r\n; notes = \r\n; nozzle_diameter = 0.4\r\n; only_retract_when_crossing_perimeters = 0\r\n; ooze_prevention = 0\r\n; output_filename_format = [input_filename_base].gcode\r\n; overhangs = 1\r\n; parking_pos_retraction = 92\r\n; pause_print_gcode = M601\r\n; perimeter_acceleration = 0\r\n; perimeter_extruder = 1\r\n; perimeter_extrusion_width = 0.45\r\n; perimeter_generator = arachne\r\n; perimeter_speed = 60\r\n; perimeters = 3\r\n; physical_printer_settings_id = \r\n; post_process = \r\n; print_settings_id = Ender3BLTouch\r\n; printer_model = \r\n; printer_notes = \r\n; printer_settings_id = Ender3BLTouch\r\n; printer_technology = FFF\r\n; printer_variant = \r\n; printer_vendor = \r\n; raft_contact_distance = 0.1\r\n; raft_expansion = 1.5\r\n; raft_first_layer_density = 90%\r\n; raft_first_layer_expansion = 3\r\n; raft_layers = 0\r\n; remaining_times = 0\r\n; resolution = 0\r\n; retract_before_travel = 2\r\n; retract_before_wipe = 0%\r\n; retract_layer_change = 0\r\n; retract_length = 2\r\n; retract_length_toolchange = 10\r\n; retract_lift = 0\r\n; retract_lift_above = 0\r\n; retract_lift_below = 0\r\n; retract_restart_extra = 0\r\n; retract_restart_extra_toolchange = 0\r\n; retract_speed = 40\r\n; seam_position = aligned\r\n; silent_mode = 1\r\n; single_extruder_multi_material = 0\r\n; single_extruder_multi_material_priming = 1\r\n; skirt_distance = 6\r\n; skirt_height = 1\r\n; skirts = 1\r\n; slice_closing_radius = 0.049\r\n; slicing_mode = regular\r\n; slowdown_below_layer_time = 5\r\n; small_perimeter_speed = 15\r\n; solid_infill_acceleration = 0\r\n; solid_infill_below_area = 70\r\n; solid_infill_every_layers = 0\r\n; solid_infill_extruder = 1\r\n; solid_infill_extrusion_width = 0.45\r\n; solid_infill_speed = 20\r\n; spiral_vase = 0\r\n; standby_temperature_delta = -5\r\n; start_filament_gcode = \"; Filament gcode\\n\"\r\n; start_gcode = G28 ; home all axes\\nG1 Z5 F5000 ; lift nozzle\\n\r\n; support_material = 0\r\n; support_material_angle = 0\r\n; support_material_auto = 1\r\n; support_material_bottom_contact_distance = 0\r\n; support_material_bottom_interface_layers = -1\r\n; support_material_buildplate_only = 0\r\n; support_material_closing_radius = 2\r\n; support_material_contact_distance = 0.2\r\n; support_material_enforce_layers = 0\r\n; support_material_extruder = 1\r\n; support_material_extrusion_width = 0.35\r\n; support_material_interface_contact_loops = 0\r\n; support_material_interface_extruder = 1\r\n; support_material_interface_layers = 3\r\n; support_material_interface_pattern = rectilinear\r\n; support_material_interface_spacing = 0\r\n; support_material_interface_speed = 100%\r\n; support_material_pattern = rectilinear\r\n; support_material_spacing = 2.5\r\n; support_material_speed = 60\r\n; support_material_style = grid\r\n; support_material_synchronize_layers = 0\r\n; support_material_threshold = 0\r\n; support_material_with_sheath = 1\r\n; support_material_xy_spacing = 50%\r\n; temperature = 215\r\n; template_custom_gcode = \r\n; thick_bridges = 1\r\n; thin_walls = 1\r\n; threads = 24\r\n; thumbnails = \r\n; thumbnails_format = PNG\r\n; toolchange_gcode = \r\n; top_fill_pattern = monotonic\r\n; top_infill_extrusion_width = 0.4\r\n; top_solid_infill_acceleration = 0\r\n; top_solid_infill_speed = 15\r\n; top_solid_layers = 3\r\n; top_solid_min_thickness = 0\r\n; travel_speed = 130\r\n; travel_speed_z = 0\r\n; use_firmware_retraction = 0\r\n; use_relative_e_distances = 0\r\n; use_volumetric_e = 0\r\n; variable_layer_height = 1\r\n; wall_distribution_count = 1\r\n; wall_transition_angle = 10\r\n; wall_transition_filter_deviation = 25%\r\n; wall_transition_length = 100%\r\n; wipe = 0\r\n; wipe_into_infill = 0\r\n; wipe_into_objects = 0\r\n; wipe_tower = 0\r\n; wipe_tower_bridging = 10\r\n; wipe_tower_brim_width = 2\r\n; wipe_tower_no_sparse_layers = 0\r\n; wipe_tower_rotation_angle = 0\r\n; wipe_tower_width = 60\r\n; wipe_tower_x = 180\r\n; wipe_tower_y = 140\r\n; wiping_volumes_extruders = 70,70\r\n; wiping_volumes_matrix = 0\r\n; xy_size_compensation = 0\r\n; z_offset = 0\r\n";
            projectData = projectData.Replace("{first_layer_height}", "" + Math.Round(base_layer_height.Value<double>(), 2).ToString());
            projectData = projectData.Replace("{layer_height}", "" + Math.Round(layer_height.Value<double>(), 2).ToString());
            JArray filament_set = new JArray();
            JProperty lastColor = null;
            foreach (JObject value in (JArray)HfpData["filament_set"])
            {
                foreach (var property in value.Properties())
                {
                    if (property.Name == "Color")
                    {
                        filament_set.AddFirst(property.Value);
                        lastColor = property;
                    }
                }
            }
            projectData = projectData.Replace("{firstColor}", "" + lastColor.Value);

            string contentXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            " <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            " <Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\"/>" +
            " <Default Extension=\"png\" ContentType=\"image/png\"/>" +
            "</Types>";

            string cutXml = await getPrusaCuttingInfo(HfpData, filament_set);

            using (FileStream zipToOpen = new FileStream(outputPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    AddToArchive(archive, "3D/3dmodel.model", stringMeshXml);
                    AddToArchive(archive, "_rels/.rels", globalRel);
                    AddToArchive(archive, "Metadata/Slic3r_PE_model.config", sliceInfoXml);
                    AddToArchive(archive, "Metadata/Slic3r_PE.config", projectData); // Slic3r_PE_model.config
                    AddToArchive(archive, "Metadata/Prusa_Slicer_custom_gcode_per_print_z.xml", cutXml);
                    AddToArchive(archive, "[Content_Types].xml", contentXml);
                }
            }
        }
    }
}