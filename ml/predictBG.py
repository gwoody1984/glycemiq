# -*- coding: utf-8 -*-
"""
Created on Sat Jun 24 16:40:41 2017

@author: Sunil
"""

import json
import pandas as pd
import numpy as np
from datetime import datetime

# input data function to read in patient data and return dataframe
def getInputData(patient_no, lag = 4*3600):
    json_data = json.loads(open('../data/patient' + str(patient_no) + '.json').read())
    bg_data = json_data['bloodGlucose']
    insulin_data = json_data['bolusInsulin']
    food_data = json_data['food']
    
    # format data
    bg_dates = pd.Series([datetime.strptime(x['readingDate'], '%Y-%m-%dT%H:%M:%S') for x in bg_data], name='raw_date')
    bg_values = pd.Series([x['bgValue']['value'] for x in bg_data], name='bg')
    bg_mealtags = [x['mealTag'] for x in bg_data]
    bg_premeal = pd.Series([1 if x=='MEAL_TAG_PRE_MEAL' else 0 for x in bg_mealtags], name='premeal')
    bg_postmeal = pd.Series([1 if x=='MEAL_TAG_POST_MEAL' else 0 for x in bg_mealtags], name='postmeal')
    bg_df = pd.concat([bg_dates, bg_values, bg_premeal, bg_postmeal], axis=1)
    bg_df['time_seconds'] = [(x-datetime(1970,1,1)).total_seconds() for x in bg_df['raw_date']]
    
    insulin_dates = pd.Series([datetime.strptime(x['readingDate'], '%Y-%m-%dT%H:%M:%S') for x in insulin_data], name='raw_date')
    insulin_values = pd.Series([x['bolusDelivered']['value'] for x in insulin_data], name='insulin_delivered')
    insulin_df = pd.concat([insulin_dates, insulin_values], axis=1)
    insulin_df['time_seconds'] = [(x-datetime(1970,1,1)).total_seconds() for x in insulin_df['raw_date']]
    
    food_dates = pd.Series([datetime.strptime(x['readingDate'], '%Y-%m-%dT%H:%M:%S') for x in food_data], name='raw_date')
    food_values = pd.Series([x['carbohydrates']['value'] for x in food_data], name='carbs')
    food_df = pd.concat([food_dates, food_values], axis=1)
    food_df['time_seconds'] = [(x-datetime(1970,1,1)).total_seconds() for x in food_df['raw_date']]
    
    bg_df['insulin'] = np.zeros(bg_df.shape[0])
    bg_df['carbs'] = np.zeros(bg_df.shape[0])
    
    # columns for amounts of insulin and food that qualify as being within
    # period of affecting blood glucose
    for i in range(bg_df.shape[0]):
        ix = (bg_df['time_seconds'][i] - insulin_df['time_seconds'] <= lag) & \
             (bg_df['time_seconds'][i] - insulin_df['time_seconds'] >= 0)
        bg_df.loc[i,'insulin'] = sum(insulin_df['insulin_delivered'][ix])
        
        ix = (bg_df['time_seconds'][i] - food_df['time_seconds'] <= lag) & \
             (bg_df['time_seconds'][i] - food_df['time_seconds'] >= 0)
        bg_df.loc[i,'carbs'] = sum(food_df['carbs'][ix])  
        
    return bg_df
    
# write kernel

# write prediction
