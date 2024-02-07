#!/bin/sh
DIR=$(dirname "$0")/../../..
TPL_DIR=$(dirname "$0")/liquids
goplay config -i $DIR/excels -oc $DIR/backend/Codes/Common/Config -od $DIR/backend/Codes/Common -p s -f -c -tc $TPL_DIR/tpl_class_conf.liquid -tm $TPL_DIR/tpl_class_manager.liquid -te $TPL_DIR/tpl_class_enum.liquid