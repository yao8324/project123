using System;

namespace ArcaneArchive.Contest
{
    public readonly struct ContestInteractionFeedback
    {
        public bool Available { get; }
        public string Action { get; }
        public string Reason { get; }
        public bool HasTarget => !string.IsNullOrEmpty(Action);
        public ContestInteractionFeedback(bool available,string action,string reason="")
        {Available=available;Action=action;Reason=reason;}
        public ContestInteractionFeedback OutOfRange()=>new ContestInteractionFeedback(false,Action,"靠近一点再交互");
        public string Prompt(string key)=>Available?"["+key+"] "+Action:Action+"  ·  "+Reason;

        public static ContestInteractionFeedback Evaluate(ContestStoryState state,string id,bool powerActive)
        {
            if(state==null || string.IsNullOrEmpty(id))return default;
            if(id.StartsWith("ARCHIVE_RECORD_SLOT_",StringComparison.Ordinal))
            {
                if(!int.TryParse(id.Substring("ARCHIVE_RECORD_SLOT_".Length),out int n) || n<1 || n>3)return default;
                return new ContestInteractionFeedback(state.stage==0,"查看档案 "+n,"先完成当前目标");
            }
            if(id=="ARCHIVE_WORK_BADGE")return new ContestInteractionFeedback(true,state.stage==0?"查看工作牌":"查看翻面的工作牌");
            if(id=="ARCHIVE_TIE_CORD")return new ContestInteractionFeedback(state.stage==0 && !state.sealedBatch,"系好蓝色封档绳",state.sealedBatch?"已经封档":"先完成当前目标");
            if(id=="ARCHIVE_TO_MAINTENANCE")return new ContestInteractionFeedback(state.stage==3,"前往下层维护厅",state.stage<3?"先完成本批档案交接":"维护已完成，先取回记录");
            if(id=="MAINTENANCE_TO_ARCHIVE")return new ContestInteractionFeedback(state.stage==3 && state.nodes==3,"返回档案馆","先稳定全部三个节点");
            if(id.StartsWith("MAINT_NODE_",StringComparison.Ordinal))
            {
                if(!int.TryParse(id.Substring("MAINT_NODE_".Length),out int n) || n<1 || n>3)return default;
                bool ready=state.stage==3 && state.nodes==n-1;
                string reason=state.nodes>=n?"节点已稳定":state.stage!=3?"先完成当前目标":"先稳定节点 "+(state.nodes+1);
                return new ContestInteractionFeedback(ready,"稳定节点 "+n,reason);
            }
            if(id=="REVEAL_CORD" && state.stage==6 && state.rescued)
                return new ContestInteractionFeedback(state.batchLocation==1,"取走本批封档包","封档包已经取走");
            if(id=="REVEAL_CORD" || id=="REVEAL_BADGE" || id=="REVEAL_WOUND")
                return new ContestInteractionFeedback(state.stage==4,id=="REVEAL_CORD"?"查看怀中的绳结":id=="REVEAL_BADGE"?"观察工作牌":"观察受伤的衣料","先按当前目标行动");
            if(id=="ARCHIVE_INCIDENT_RECORD")return new ContestInteractionFeedback(state.stage==5,"查看事故抄件","先核对三项身份线索");
            if(id=="RESCUE_CONDUIT")return new ContestInteractionFeedback(state.stage==6 && !state.rescued,powerActive?"按住引导，松开收束":"引导回路",state.rescued?"回路已稳定，取走封档包":"先确认自己的身份");
            if(id=="ARCHIVE_TRANSFER_CASE")return new ContestInteractionFeedback(state.stage==6 && state.batchLocation==2,"交付本批遗赠",state.transferred?"遗赠已经安全转移":"先稳住回路，再取走封档包");
            return default;
        }
    }
}
