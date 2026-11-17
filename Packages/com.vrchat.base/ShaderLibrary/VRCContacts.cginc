#ifndef VRCCONTACTS_CGINC
#define VRCCONTACTS_CGINC

#define VRC_CONTACT_MAX_TARGETS 8
#define VRC_CONTACT_MAX_TAGS 16
#define VRC_CONTACT_BUFFER_SIZE 9 // MAX_TARGETS, plus one extra for SELF
#define VRC_CONTACT_SELF_INDEX VRC_CONTACT_MAX_TARGETS

/*
	Material Contact data buffer specification:

    float4x4 _VRChatContact_Transforms_NAME[] contains localToWorld transforms for the
		collision shapes of contacting senders, in descending order of proximity,
		and the last index contains that of the receiver's root transform (not its collision shape)
    float4 _VRChatContact_Data_NAME[] is parallel to _Transforms but contains packed data for those contact senders.
        Data_...[i].x contains the playerId of the contact's owner, as an int32
        Data_...[i].y contains a bit-flag array of which of the receiver's
			contact tags are matched by the sender, as a uint32
		Data_...[i].y at the last index contains the current number of targets in the buffer, as an int32
        Data_...[i].z and .w are reserved and their values are undefined

	To access the above data, the following macros are defined for your convenience.
*/

#define VRC_CONTACT_DECLARE(name) \
uniform float4x4 _VRChatContact_Transforms_##name[VRC_CONTACT_BUFFER_SIZE]; \
uniform float4 _VRChatContact_Data_##name[VRC_CONTACT_BUFFER_SIZE]

#define VRC_CONTACT_TARGETCOUNT(name) (asint(_VRChatContact_Data_##name[VRC_CONTACT_SELF_INDEX].y))

#define VRC_CONTACT_TRANSFORM(name, i) (_VRChatContact_Transforms_##name[i])
#define VRC_CONTACT_TRANSFORM_SELF(name) VRC_CONTACT_TRANSFORM(name, VRC_CONTACT_SELF_INDEX)

#define VRC_CONTACT_WORLDPOS(name, i) (_VRChatContact_Transforms_##name[i]._m03_m13_m23)
#define VRC_CONTACT_WORLDPOS_SELF(name) VRC_CONTACT_WORLDPOS(name, VRC_CONTACT_SELF_INDEX)

#define VRC_CONTACT_PLAYERID(name, i) (asint(_VRChatContact_Data_##name[i].x))
#define VRC_CONTACT_PLAYERID_SELF(name) VRC_CONTACT_PLAYERID(name, VRC_CONTACT_SELF_INDEX)

#define VRC_CONTACT_TAGS(name, i) (asuint(_VRChatContact_Data_##name[i].y))
#define VRC_CONTACT_GET_TAG(name, i, tagIndex) ((VRC_CONTACT_TAGS(name, i) >> tagIndex) & 1)

#endif // VRCCONTACTS_CGINC
