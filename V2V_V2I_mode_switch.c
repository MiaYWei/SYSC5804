#include <stdbool.h>
#include <time.h>
#include <windows.h>

typedef enum {RSU, VEHICLE_A, VEHICLE_B, VEHICLE_C, ALL, INVALID_ID} id;
typedef enum {UP, DOWN} drive_direction;
typedef enum {BITERROR, OTHER_ERR, NO_ERR} error_code;
typedef enum {V2V, V2I, V2V_V2I, INVALID} v2x_mode;
typedef struct
{
    float location[2];
    drive_direction direction;
    bool safety_alert;
    int sender_id;
    int receiver_id;
    DWORD start_time;
    DWORD received_time;
    error_code errocode;
} message;

#define ASSUME_VEHICLE_SPEED 25     /*Assume value here. Can extract speed info from speed senor device, eg: m/sec*/
#define ASSUME_ALTITUDE   20.0      /*Assume value here. Can extract location info from GPS device*/
#define ASSUME_LONGITUTE  30.0
    
/*Global variabl: default mode is V2V_V2I, indicates the specific vehicle listen to RSU and other vehicles synchronous*/
v2x_mode mode = V2V_V2I; 

/**
 * @brief Receiver checks the received BCM message(physical and datalink layer).
 * 
 * @param msg the received message
 * @return true  message received succesfully without error
 * @return false message will be discarded, since error happens during the transmission.
 */
bool process_receive_messsge(message msg)
{
    int discard_msg = 0;
    int received_BCM = 0;
    float ratio;
    if (msg.sender_id != RSU)
    {
        return false;
    }

    if ((msg.errocode == BITERROR) || (msg.errocode == OTHER_ERR))
    {
        discard_msg++;
    }
    else if ((msg.errocode == NO_ERR) && (msg.safety_alert = true))
    {
        return true;
    }
    
    return false;
}

/**
 * @brief Once safety alert message received the specific vehicle (eg:VEHICLE_B),
 * then switch mode to V2V, broadcast alert to all other vehicles
 * 
 */
void V2V_broadcast_alert_message(void)
{
    message msg;
    struct timeval tv;
    gettimeofday(&tv, NULL);
    time_t curret_time;
    
    /*switch mode to V2V, and broadcast alert to all other vehicles*/
    mode = V2V; 
    msg.sender_id = VEHICLE_B; 
    msg.receiver_id = ALL;
    msg.safety_alert = true;
    msg.start_time = tv.tv_sec * 1000 + tv.tv_usec / 1000; //ms

    return;
}

/**
 * @brief The specific vehicle (eg:VEHICLE_B) wants to update current info (location, speed, direction) to RSU
 *  switch mode to V2V, then send message to RSU
 * 
 */
void V2I_update_messsge_to_RSU(void)
{
    message msg;
    float gsp_location[2] = {ASSUME_ALTITUDE, ASSUME_LONGITUTE};                                
    struct timeval tv;
    gettimeofday(&tv, NULL);                                
    
    /*switch mode to V2I, and update current info (location, speed, direction) to RSU*/
    mode = V2I; 

    msg.sender_id = VEHICLE_B;
    msg.receiver_id = RSU;
    memcpy(&msg.location, gsp_location, sizeof(gsp_location)); 
    msg.direction = UP;
    msg.direction = ASSUME_VEHICLE_SPEED;
    msg.start_time = tv.tv_sec * 1000 + tv.tv_usec / 1000; //ms
    
    return;
}

/**
 * @brief This is the main function for V2V, V2I mode switch. 
 *  The specific vehicle periodically receive massage from RSU and other vehicle 
 *  If alert message is received, then it switchs to V2V mode to broadcast the alert messsge to all other vehicles; 
 *  After that, switch to V2I mode to update its location, speed, and direction info to RSU 
 * 
 * @param msg the received message 
 */
void V2V_V2I_mode_switch(message msg)
{   
    /* the vehicle periodically receive massage from RSU and other vehicle*/
    if (process_receive_messsge(msg))
    {
        V2V_broadcast_alert_message();
    }
    
    V2I_update_messsge_to_RSU();
    
    /*Setback to default mode, to listen to RSU and other vehicles synchronous*/
    mode = V2V_V2I;

    return;
}

void main (message msg)
{
    V2V_V2I_mode_switch(msg);

    return;
}