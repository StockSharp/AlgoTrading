// 'cache' is an instance of layout cache
// 'type' should be descendant of Notifiable

#define ON_EVENT_LAYOUT_INDEX(event, cache, controlIndex, handler)  if(id == (event + CHARTEVENT_CUSTOM) && lparam == cache.get(controlIndex).Id()) { handler(); return(true); }
#define ON_EVENT_LAYOUT_ARRAY(event, cache)  if(id == (event + CHARTEVENT_CUSTOM) && cache.onEvent(event, cache.get(-lparam))) { return true; }
#define ON_EVENT_LAYOUT_CTRL_ANY(event, cache, type, anything)  if(id == (event + CHARTEVENT_CUSTOM)) {type *ptr = dynamic_cast<type *>(cache.get(-lparam)); if(ptr != NULL && ptr.onEvent(event, anything)) { return true; }}
#define ON_EVENT_LAYOUT_CTRL_DLG(event, cache, type)  if(id == (event + CHARTEVENT_CUSTOM)) {type *ptr = dynamic_cast<type *>(cache.get(-lparam)); if(ptr != NULL && ptr.onEvent(event, &this)) { return true; }}
#define ON_EVENT_RAW(event, handler)  if(id == event) { handler(lparam, dparam, sparam); return(true); }

#define ON_LAYOUT_REFRESH 99 // "panic" event id

#define PRT(A) Print(#A, " ", (A))
