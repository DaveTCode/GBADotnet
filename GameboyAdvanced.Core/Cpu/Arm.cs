using System.Numerics;

namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
    // To decode an arm 32 bit instruction we can
    // 1. Skip bits 0-3 (usually RM/Offset)
    // 2. Skip condition bits 28-31
    // 3. Skip bits 8-19 as never used to disambiguate between operations
    // End result is bits 4-7 and 20-27 which is 12 bits or 0xFFF possible values
    // We map all of those into this array assuming that the cache pressure is fine from
    // loading 0xFFFF * 4 bytes (per delegate*) despite that being > typical L1 cache size
    internal readonly static delegate*<Core, uint, void>[] InstructionMap =
    {
        &and_lli,&and_llr,&and_lri,&and_lrr,&and_ari,&and_arr,&and_rri,&and_rrr,&and_lli,&mul,&and_lri,&strh_ptrm,&and_ari,&undefined,&and_rri,&undefined,
        &ands_lli,&ands_llr,&ands_lri,&ands_lrr,&ands_ari,&ands_arr,&ands_rri,&ands_rrr,&ands_lli,&muls,&ands_lri,&ldrh_ptrm,&ands_ari,&ldrsb_ptrm,&ands_rri,&ldrsh_ptrm,
        &eor_lli,&eor_llr,&eor_lri,&eor_lrr,&eor_ari,&eor_arr,&eor_rri,&eor_rrr,&eor_lli,&mla,&eor_lri,&strh_ptrm,&eor_ari,&undefined,&eor_rri,&undefined,
        &eors_lli,&eors_llr,&eors_lri,&eors_lrr,&eors_ari,&eors_arr,&eors_rri,&eors_rrr,&eors_lli,&mlas,&eors_lri,&ldrh_ptrm,&eors_ari,&ldrsb_ptrm,&eors_rri,&ldrsh_ptrm,
        &sub_lli,&sub_llr,&sub_lri,&sub_lrr,&sub_ari,&sub_arr,&sub_rri,&sub_rrr,&sub_lli,&undefined,&sub_lri,&strh_ptim,&sub_ari,&undefined,&sub_rri,&undefined,
        &subs_lli,&subs_llr,&subs_lri,&subs_lrr,&subs_ari,&subs_arr,&subs_rri,&subs_rrr,&subs_lli,&undefined,&subs_lri,&ldrh_ptim,&subs_ari,&ldrsb_ptim,&subs_rri,&ldrsh_ptim,
        &rsb_lli,&rsb_llr,&rsb_lri,&rsb_lrr,&rsb_ari,&rsb_arr,&rsb_rri,&rsb_rrr,&rsb_lli,&undefined,&rsb_lri,&strh_ptim,&rsb_ari,&undefined,&rsb_rri,&undefined,
        &rsbs_lli,&rsbs_llr,&rsbs_lri,&rsbs_lrr,&rsbs_ari,&rsbs_arr,&rsbs_rri,&rsbs_rrr,&rsbs_lli,&undefined,&rsbs_lri,&ldrh_ptim,&rsbs_ari,&ldrsb_ptim,&rsbs_rri,&ldrsh_ptim,
        &add_lli,&add_llr,&add_lri,&add_lrr,&add_ari,&add_arr,&add_rri,&add_rrr,&add_lli,&umull,&add_lri,&strh_ptrp,&add_ari,&undefined,&add_rri,&undefined,
        &adds_lli,&adds_llr,&adds_lri,&adds_lrr,&adds_ari,&adds_arr,&adds_rri,&adds_rrr,&adds_lli,&umulls,&adds_lri,&ldrh_ptrp,&adds_ari,&ldrsb_ptrp,&adds_rri,&ldrsh_ptrp,
        &adc_lli,&adc_llr,&adc_lri,&adc_lrr,&adc_ari,&adc_arr,&adc_rri,&adc_rrr,&adc_lli,&umlal,&adc_lri,&strh_ptrp,&adc_ari,&undefined,&adc_rri,&undefined,
        &adcs_lli,&adcs_llr,&adcs_lri,&adcs_lrr,&adcs_ari,&adcs_arr,&adcs_rri,&adcs_rrr,&adcs_lli,&umlals,&adcs_lri,&ldrh_ptrp,&adcs_ari,&ldrsb_ptrp,&adcs_rri,&ldrsh_ptrp,
        &sbc_lli,&sbc_llr,&sbc_lri,&sbc_lrr,&sbc_ari,&sbc_arr,&sbc_rri,&sbc_rrr,&sbc_lli,&smull,&sbc_lri,&strh_ptip,&sbc_ari,&undefined,&sbc_rri,&undefined,
        &sbcs_lli,&sbcs_llr,&sbcs_lri,&sbcs_lrr,&sbcs_ari,&sbcs_arr,&sbcs_rri,&sbcs_rrr,&sbcs_lli,&smulls,&sbcs_lri,&ldrh_ptip,&sbcs_ari,&ldrsb_ptip,&sbcs_rri,&ldrsh_ptip,
        &rsc_lli,&rsc_llr,&rsc_lri,&rsc_lrr,&rsc_ari,&rsc_arr,&rsc_rri,&rsc_rrr,&rsc_lli,&smlal,&rsc_lri,&strh_ptip,&rsc_ari,&undefined,&rsc_rri,&undefined,
        &rscs_lli,&rscs_llr,&rscs_lri,&rscs_lrr,&rscs_ari,&rscs_arr,&rscs_rri,&rscs_rrr,&rscs_lli,&smlals,&rscs_lri,&ldrh_ptip,&rscs_ari,&ldrsb_ptip,&rscs_rri,&ldrsh_ptip,
        &mrs_rc,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&swp,&undefined,&strh_ofrm,&undefined,&undefined,&undefined,&undefined,
        &tsts_lli,&tsts_llr,&tsts_lri,&tsts_lrr,&tsts_ari,&tsts_arr,&tsts_rri,&tsts_rrr,&tsts_lli,&undefined,&tsts_lri,&ldrh_ofrm,&tsts_ari,&ldrsb_ofrm,&tsts_rri,&ldrsh_ofrm,
        &msr_rc,&bx,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&strh_prrm,&undefined,&undefined,&undefined,&undefined,
        &teqs_lli,&teqs_llr,&teqs_lri,&teqs_lrr,&teqs_ari,&teqs_arr,&teqs_rri,&teqs_rrr,&teqs_lli,&undefined,&teqs_lri,&ldrh_prrm,&teqs_ari,&ldrsb_prrm,&teqs_rri,&ldrsh_prrm,
        &mrs_rs,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&swpb,&undefined,&strh_ofim,&undefined,&undefined,&undefined,&undefined,
        &cmps_lli,&cmps_llr,&cmps_lri,&cmps_lrr,&cmps_ari,&cmps_arr,&cmps_rri,&cmps_rrr,&cmps_lli,&undefined,&cmps_lri,&ldrh_ofim,&cmps_ari,&ldrsb_ofim,&cmps_rri,&ldrsh_ofim,
        &msr_rs,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&strh_prim,&undefined,&undefined,&undefined,&undefined,
        &cmns_lli,&cmns_llr,&cmns_lri,&cmns_lrr,&cmns_ari,&cmns_arr,&cmns_rri,&cmns_rrr,&cmns_lli,&undefined,&cmns_lri,&ldrh_prim,&cmns_ari,&ldrsb_prim,&cmns_rri,&ldrsh_prim,
        &orr_lli,&orr_llr,&orr_lri,&orr_lrr,&orr_ari,&orr_arr,&orr_rri,&orr_rrr,&orr_lli,&undefined,&orr_lri,&strh_ofrp,&orr_ari,&undefined,&orr_rri,&undefined,
        &orrs_lli,&orrs_llr,&orrs_lri,&orrs_lrr,&orrs_ari,&orrs_arr,&orrs_rri,&orrs_rrr,&orrs_lli,&undefined,&orrs_lri,&ldrh_ofrp,&orrs_ari,&ldrsb_ofrp,&orrs_rri,&ldrsh_ofrp,
        &mov_lli,&mov_llr,&mov_lri,&mov_lrr,&mov_ari,&mov_arr,&mov_rri,&mov_rrr,&mov_lli,&undefined,&mov_lri,&strh_prrp,&mov_ari,&undefined,&mov_rri,&undefined,
        &movs_lli,&movs_llr,&movs_lri,&movs_lrr,&movs_ari,&movs_arr,&movs_rri,&movs_rrr,&movs_lli,&undefined,&movs_lri,&ldrh_prrp,&movs_ari,&ldrsb_prrp,&movs_rri,&ldrsh_prrp,
        &bic_lli,&bic_llr,&bic_lri,&bic_lrr,&bic_ari,&bic_arr,&bic_rri,&bic_rrr,&bic_lli,&undefined,&bic_lri,&strh_ofip,&bic_ari,&undefined,&bic_rri,&undefined,
        &bics_lli,&bics_llr,&bics_lri,&bics_lrr,&bics_ari,&bics_arr,&bics_rri,&bics_rrr,&bics_lli,&undefined,&bics_lri,&ldrh_ofip,&bics_ari,&ldrsb_ofip,&bics_rri,&ldrsh_ofip,
        &mvn_lli,&mvn_llr,&mvn_lri,&mvn_lrr,&mvn_ari,&mvn_arr,&mvn_rri,&mvn_rrr,&mvn_lli,&undefined,&mvn_lri,&strh_prip,&mvn_ari,&undefined,&mvn_rri,&undefined,
        &mvns_lli,&mvns_llr,&mvns_lri,&mvns_lrr,&mvns_ari,&mvns_arr,&mvns_rri,&mvns_rrr,&mvns_lli,&undefined,&mvns_lri,&ldrh_prip,&mvns_ari,&ldrsb_prip,&mvns_rri,&ldrsh_prip,
        &and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,&and_imm,
        &ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,&ands_imm,
        &eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,&eor_imm,
        &eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,&eors_imm,
        &sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,&sub_imm,
        &subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,&subs_imm,
        &rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,&rsb_imm,
        &rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,&rsbs_imm,
        &add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,&add_imm,
        &adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,&adds_imm,
        &adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,&adc_imm,
        &adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,&adcs_imm,
        &sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,&sbc_imm,
        &sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,&sbcs_imm,
        &rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,&rsc_imm,
        &rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,&rscs_imm,
        &undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,
        &tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,&tsts_imm,
        &msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,&msr_ic,
        &teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,&teqs_imm,
        &undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,&undefined,
        &cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,&cmps_imm,
        &msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,&msr_is,
        &cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,&cmns_imm,
        &orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,&orr_imm,
        &orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,&orrs_imm,
        &mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,&mov_imm,
        &movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,&movs_imm,
        &bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,&bic_imm,
        &bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,&bics_imm,
        &mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,&mvn_imm,
        &mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,&mvns_imm,
        &str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,
        &ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,
        &str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,&str_ptim,
        &ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,&ldr_ptim,
        &strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,
        &ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,
        &strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,&strb_ptim,
        &ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,&ldrb_ptim,
        &str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,
        &ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,
        &str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,&str_ptip,
        &ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,&ldr_ptip,
        &strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,
        &ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,
        &strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,&strb_ptip,
        &ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,&ldrb_ptip,
        &str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,&str_ofim,
        &ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,&ldr_ofim,
        &str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,&str_prim,
        &ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,&ldr_prim,
        &strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,&strb_ofim,
        &ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,&ldrb_ofim,
        &strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,&strb_prim,
        &ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,&ldrb_prim,
        &str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,&str_ofip,
        &ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,&ldr_ofip,
        &str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,&str_prip,
        &ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,&ldr_prip,
        &strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,&strb_ofip,
        &ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,&ldrb_ofip,
        &strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,&strb_prip,
        &ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,&ldrb_prip,
        &str_ptrmll,&undefined,&str_ptrmlr,&undefined,&str_ptrmar,&undefined,&str_ptrmrr,&undefined,&str_ptrmll,&undefined,&str_ptrmlr,&undefined,&str_ptrmar,&undefined,&str_ptrmrr,&undefined,
        &ldr_ptrmll,&undefined,&ldr_ptrmlr,&undefined,&ldr_ptrmar,&undefined,&ldr_ptrmrr,&undefined,&ldr_ptrmll,&undefined,&ldr_ptrmlr,&undefined,&ldr_ptrmar,&undefined,&ldr_ptrmrr,&undefined,
        &str_ptrmll,&undefined,&str_ptrmlr,&undefined,&str_ptrmar,&undefined,&str_ptrmrr,&undefined,&str_ptrmll,&undefined,&str_ptrmlr,&undefined,&str_ptrmar,&undefined,&str_ptrmrr,&undefined,
        &ldr_ptrmll,&undefined,&ldr_ptrmlr,&undefined,&ldr_ptrmar,&undefined,&ldr_ptrmrr,&undefined,&ldr_ptrmll,&undefined,&ldr_ptrmlr,&undefined,&ldr_ptrmar,&undefined,&ldr_ptrmrr,&undefined,
        &strb_ptrmll,&undefined,&strb_ptrmlr,&undefined,&strb_ptrmar,&undefined,&strb_ptrmrr,&undefined,&strb_ptrmll,&undefined,&strb_ptrmlr,&undefined,&strb_ptrmar,&undefined,&strb_ptrmrr,&undefined,
        &ldrb_ptrmll,&undefined,&ldrb_ptrmlr,&undefined,&ldrb_ptrmar,&undefined,&ldrb_ptrmrr,&undefined,&ldrb_ptrmll,&undefined,&ldrb_ptrmlr,&undefined,&ldrb_ptrmar,&undefined,&ldrb_ptrmrr,&undefined,
        &strb_ptrmll,&undefined,&strb_ptrmlr,&undefined,&strb_ptrmar,&undefined,&strb_ptrmrr,&undefined,&strb_ptrmll,&undefined,&strb_ptrmlr,&undefined,&strb_ptrmar,&undefined,&strb_ptrmrr,&undefined,
        &ldrb_ptrmll,&undefined,&ldrb_ptrmlr,&undefined,&ldrb_ptrmar,&undefined,&ldrb_ptrmrr,&undefined,&ldrb_ptrmll,&undefined,&ldrb_ptrmlr,&undefined,&ldrb_ptrmar,&undefined,&ldrb_ptrmrr,&undefined,
        &str_ptrpll,&undefined,&str_ptrplr,&undefined,&str_ptrpar,&undefined,&str_ptrprr,&undefined,&str_ptrpll,&undefined,&str_ptrplr,&undefined,&str_ptrpar,&undefined,&str_ptrprr,&undefined,
        &ldr_ptrpll,&undefined,&ldr_ptrplr,&undefined,&ldr_ptrpar,&undefined,&ldr_ptrprr,&undefined,&ldr_ptrpll,&undefined,&ldr_ptrplr,&undefined,&ldr_ptrpar,&undefined,&ldr_ptrprr,&undefined,
        &str_ptrpll,&undefined,&str_ptrplr,&undefined,&str_ptrpar,&undefined,&str_ptrprr,&undefined,&str_ptrpll,&undefined,&str_ptrplr,&undefined,&str_ptrpar,&undefined,&str_ptrprr,&undefined,
        &ldr_ptrpll,&undefined,&ldr_ptrplr,&undefined,&ldr_ptrpar,&undefined,&ldr_ptrprr,&undefined,&ldr_ptrpll,&undefined,&ldr_ptrplr,&undefined,&ldr_ptrpar,&undefined,&ldr_ptrprr,&undefined,
        &strb_ptrpll,&undefined,&strb_ptrplr,&undefined,&strb_ptrpar,&undefined,&strb_ptrprr,&undefined,&strb_ptrpll,&undefined,&strb_ptrplr,&undefined,&strb_ptrpar,&undefined,&strb_ptrprr,&undefined,
        &ldrb_ptrpll,&undefined,&ldrb_ptrplr,&undefined,&ldrb_ptrpar,&undefined,&ldrb_ptrprr,&undefined,&ldrb_ptrpll,&undefined,&ldrb_ptrplr,&undefined,&ldrb_ptrpar,&undefined,&ldrb_ptrprr,&undefined,
        &strb_ptrpll,&undefined,&strb_ptrplr,&undefined,&strb_ptrpar,&undefined,&strb_ptrprr,&undefined,&strb_ptrpll,&undefined,&strb_ptrplr,&undefined,&strb_ptrpar,&undefined,&strb_ptrprr,&undefined,
        &ldrb_ptrpll,&undefined,&ldrb_ptrplr,&undefined,&ldrb_ptrpar,&undefined,&ldrb_ptrprr,&undefined,&ldrb_ptrpll,&undefined,&ldrb_ptrplr,&undefined,&ldrb_ptrpar,&undefined,&ldrb_ptrprr,&undefined,
        &str_ofrmll,&undefined,&str_ofrmlr,&undefined,&str_ofrmar,&undefined,&str_ofrmrr,&undefined,&str_ofrmll,&undefined,&str_ofrmlr,&undefined,&str_ofrmar,&undefined,&str_ofrmrr,&undefined,
        &ldr_ofrmll,&undefined,&ldr_ofrmlr,&undefined,&ldr_ofrmar,&undefined,&ldr_ofrmrr,&undefined,&ldr_ofrmll,&undefined,&ldr_ofrmlr,&undefined,&ldr_ofrmar,&undefined,&ldr_ofrmrr,&undefined,
        &str_prrmll,&undefined,&str_prrmlr,&undefined,&str_prrmar,&undefined,&str_prrmrr,&undefined,&str_prrmll,&undefined,&str_prrmlr,&undefined,&str_prrmar,&undefined,&str_prrmrr,&undefined,
        &ldr_prrmll,&undefined,&ldr_prrmlr,&undefined,&ldr_prrmar,&undefined,&ldr_prrmrr,&undefined,&ldr_prrmll,&undefined,&ldr_prrmlr,&undefined,&ldr_prrmar,&undefined,&ldr_prrmrr,&undefined,
        &strb_ofrmll,&undefined,&strb_ofrmlr,&undefined,&strb_ofrmar,&undefined,&strb_ofrmrr,&undefined,&strb_ofrmll,&undefined,&strb_ofrmlr,&undefined,&strb_ofrmar,&undefined,&strb_ofrmrr,&undefined,
        &ldrb_ofrmll,&undefined,&ldrb_ofrmlr,&undefined,&ldrb_ofrmar,&undefined,&ldrb_ofrmrr,&undefined,&ldrb_ofrmll,&undefined,&ldrb_ofrmlr,&undefined,&ldrb_ofrmar,&undefined,&ldrb_ofrmrr,&undefined,
        &strb_prrmll,&undefined,&strb_prrmlr,&undefined,&strb_prrmar,&undefined,&strb_prrmrr,&undefined,&strb_prrmll,&undefined,&strb_prrmlr,&undefined,&strb_prrmar,&undefined,&strb_prrmrr,&undefined,
        &ldrb_prrmll,&undefined,&ldrb_prrmlr,&undefined,&ldrb_prrmar,&undefined,&ldrb_prrmrr,&undefined,&ldrb_prrmll,&undefined,&ldrb_prrmlr,&undefined,&ldrb_prrmar,&undefined,&ldrb_prrmrr,&undefined,
        &str_ofrpll,&undefined,&str_ofrplr,&undefined,&str_ofrpar,&undefined,&str_ofrprr,&undefined,&str_ofrpll,&undefined,&str_ofrplr,&undefined,&str_ofrpar,&undefined,&str_ofrprr,&undefined,
        &ldr_ofrpll,&undefined,&ldr_ofrplr,&undefined,&ldr_ofrpar,&undefined,&ldr_ofrprr,&undefined,&ldr_ofrpll,&undefined,&ldr_ofrplr,&undefined,&ldr_ofrpar,&undefined,&ldr_ofrprr,&undefined,
        &str_prrpll,&undefined,&str_prrplr,&undefined,&str_prrpar,&undefined,&str_prrprr,&undefined,&str_prrpll,&undefined,&str_prrplr,&undefined,&str_prrpar,&undefined,&str_prrprr,&undefined,
        &ldr_prrpll,&undefined,&ldr_prrplr,&undefined,&ldr_prrpar,&undefined,&ldr_prrprr,&undefined,&ldr_prrpll,&undefined,&ldr_prrplr,&undefined,&ldr_prrpar,&undefined,&ldr_prrprr,&undefined,
        &strb_ofrpll,&undefined,&strb_ofrplr,&undefined,&strb_ofrpar,&undefined,&strb_ofrprr,&undefined,&strb_ofrpll,&undefined,&strb_ofrplr,&undefined,&strb_ofrpar,&undefined,&strb_ofrprr,&undefined,
        &ldrb_ofrpll,&undefined,&ldrb_ofrplr,&undefined,&ldrb_ofrpar,&undefined,&ldrb_ofrprr,&undefined,&ldrb_ofrpll,&undefined,&ldrb_ofrplr,&undefined,&ldrb_ofrpar,&undefined,&ldrb_ofrprr,&undefined,
        &strb_prrpll,&undefined,&strb_prrplr,&undefined,&strb_prrpar,&undefined,&strb_prrprr,&undefined,&strb_prrpll,&undefined,&strb_prrplr,&undefined,&strb_prrpar,&undefined,&strb_prrprr,&undefined,
        &ldrb_prrpll,&undefined,&ldrb_prrplr,&undefined,&ldrb_prrpar,&undefined,&ldrb_prrprr,&undefined,&ldrb_prrpll,&undefined,&ldrb_prrplr,&undefined,&ldrb_prrpar,&undefined,&ldrb_prrprr,&undefined,
        &stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,&stmda,
        &ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,&ldmda,
        &stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,&stmda_w,
        &ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,&ldmda_w,
        &stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,&stmda_u,
        &ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,&ldmda_u,
        &stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,&stmda_uw,
        &ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,&ldmda_uw,
        &stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,&stmia,
        &ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,&ldmia,
        &stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,&stmia_w,
        &ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,&ldmia_w,
        &stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,&stmia_u,
        &ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,&ldmia_u,
        &stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,&stmia_uw,
        &ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,&ldmia_uw,
        &stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,&stmdb,
        &ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,&ldmdb,
        &stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,&stmdb_w,
        &ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,&ldmdb_w,
        &stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,&stmdb_u,
        &ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,&ldmdb_u,
        &stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,&stmdb_uw,
        &ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,&ldmdb_uw,
        &stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,&stmib,
        &ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,&ldmib,
        &stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,&stmib_w,
        &ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,&ldmib_w,
        &stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,&stmib_u,
        &ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,&ldmib_u,
        &stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,&stmib_uw,
        &ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,&ldmib_uw,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,&b,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,&bl,
        &stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,
        &ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,
        &stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,
        &ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,
        &stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,&stc_ofm,
        &ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,&ldc_ofm,
        &stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,&stc_prm,
        &ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,&ldc_prm,
        &stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,
        &ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,
        &stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,
        &ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,
        &stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,&stc_ofp,
        &ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,&ldc_ofp,
        &stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,&stc_prp,
        &ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,&ldc_prp,
        &stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,
        &ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,
        &stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,
        &ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,
        &stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,&stc_unm,
        &ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,&ldc_unm,
        &stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,&stc_ptm,
        &ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,&ldc_ptm,
        &stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,
        &ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,
        &stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,
        &ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,
        &stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,&stc_unp,
        &ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,&ldc_unp,
        &stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,&stc_ptp,
        &ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,&ldc_ptp,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,&cdp,&mcr,
        &cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,&cdp,&mrc,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
        &swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,&swi,
    };

#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// During an LDR operation cycle 2 is always writing the value on the 
    /// data bus back onto the appropriate destination register.
    /// </summary>
    internal static void ldr_writeback(Core core, uint instruction)
    {
        var rd = (instruction >> 12) & 0b1111;

        // TODO - "A byte load (LDRB) expects the data on data bus inputs 7 through 0 if the supplied
        // address is on a word boundary, on data bus inputs 15 through 8 if it is a word address
        // plus one byte, and so on. The selected byte is placed in the bottom 8 bits of the
        // destination register, and the remaining bits of the register are filled with zeros.
        //
        // This doesn't seem to actually work, the data we want is really on D in little endian orientation already.
        // Is the data bus (D) stored in big endian format regardless of processor mode and then the memory unit flips it?
        // var rotateResultBits = (int)((core.A & 0b11) * 8);
        // var rotatedData = (core.D >> rotateResultBits) | (core.D << (32 - rotateResultBits));

        core.R[rd] = core.MAS switch
        {
            BusWidth.Word => core.D,
            BusWidth.HalfWord => core.D & 0xFFFF, // TODO - Is it true that LDRH behaves the same w.r.t. word mis-aligned reads?
            BusWidth.Byte => core.D & 0xFF,
            _ => throw new Exception($"Invalid bus width {core.MAS}"),
        };

        if (rd == 15) core.ClearPipeline();

        Core.ResetMemoryUnitForArmOpcodeFetch(core, instruction);
    }

    internal static void SingleDataTransfer(Core core, uint instruction)
    {
        var i = ((instruction >> 25) & 0b1) == 1;
        var p = ((instruction >> 24) & 0b1) == 1;
        var u = ((instruction >> 23) & 0b1) == 1;
        var b = ((instruction >> 22) & 0b1) == 1;
        var w = ((instruction >> 21) & 0b1) == 1;
        var l = ((instruction >> 20) & 0b1) == 1;
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;
        var offsetField = instruction & 0b1111_1111_1111;
        uint offset;

        if (i)
        {
            offset = offsetField;
        }
        else
        {
            var rm = offsetField & 0b1111;
            var shiftType = (offsetField >> 4) & 0b11;
            var shiftAmount = (byte)((offsetField >> 7) & 0b1_1111);

            offset = shiftType switch
            {
                0b00 => ALU.LSLNoFlags(core.R[rm], shiftAmount),
                0b01 => ALU.LSRNoFlags(core.R[rm], shiftAmount),
                0b10 => ALU.ASRNoFlags(core.R[rm], shiftAmount),
                0b11 => ALU.RORNoFlags(core.R[rm], shiftAmount),
                _ => throw new Exception(),
            };
        }

        core.A = (p, u) switch
        {
            (true, true) => core.R[rn] + offset,
            (true, false) => core.R[rn] - offset,
            (false, _) => core.R[rn]
        };

        if (w)
        {
            core.R[rn] = core.A;
        }

        if (b)
        {
            core.MAS = BusWidth.Byte;
        }
        else
        {
            core.MAS = BusWidth.Word;
        }

        if (l)
        {
            core.nOPC = true;
            core.nRW = false;
            core.SEQ = false;
            core.NextExecuteAction = &ldr_writeback;
        }
        else
        {
            if (b)
            {
                // "A byte store (STRB) repeats the bottom 8 bits of the source register four times across
                // data bus outputs 31 through 0.The external memory system should activate the
                // appropriate byte subsystem to store the data"
                // From the ARM7TDMI manual
                core.D = (core.R[rd] & 0xFF) | ((core.R[rd] & 0xFF) << 8) | ((core.R[rd] & 0xFF) << 16) | ((core.R[rd] & 0xFF) << 24);
            }
            else
            {
                // "A word store (STR) should generate a word aligned address. The word presented to
                // the data bus is not affected if the address is not word aligned. That is, bit 31 of the
                // register being stored always appears on data bus output 31."
                // From the ARM7TDMI manual
                core.D = core.R[rd];
            }

            core.nOPC = true;
            core.nRW = true;
            core.SEQ = false;
            core.NextExecuteAction = &Core.ResetMemoryUnitForArmOpcodeFetch;
        }
    }

    /// <summary>
    /// Unused but leaving here in case I want to compare costs of running this 
    /// vs the auto generated code
    /// </summary>
    internal static void data_op(Core core, uint instruction)
    {
        var is_immediate = (instruction >> 25) & 0b1;
        var opcode = (instruction >> 21) & 0b1111;
        var s = (instruction >> 20) & 0b1;
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;

        uint secondOperand;
        if (is_immediate == 1)
        {
            var imm = instruction & 0b1111_1111;
            var rot = ((instruction >> 8) & 0b1111) * 2;
            secondOperand = ALU.RORNoFlags(imm, (byte)rot); // TODO - Does carry get set to output of ROR from ALU?
        }
        else
        {
            var shift = (instruction >> 4) & 0b1111_1111;
            var rm = instruction & 0b1111;
            var useRegister = shift & 0b1;
            var shiftType = (shift >> 1) & 0b11;

            secondOperand = (s, useRegister, shiftType) switch
            {
                (0, 0, 0b00) => ALU.LSLNoFlags(core.R[rm], (byte)((shift >> 7) & 0b1_1111)),
                (0, 0, 0b01) => ALU.LSRNoFlags(core.R[rm], (byte)((shift >> 7) & 0b1_1111)),
                (0, 0, 0b10) => ALU.ASRNoFlags(core.R[rm], (byte)((shift >> 7) & 0b1_1111)),
                (0, 0, 0b11) => ALU.RORNoFlags(core.R[rm], (byte)((shift >> 7) & 0b1_1111)),
                (1, 0, 0b00) => ALU.LSL(core.R[rm], (byte)((shift >> 7) & 0b1_1111), ref core.Cpsr),
                (1, 0, 0b01) => ALU.LSR(core.R[rm], (byte)((shift >> 7) & 0b1_1111), ref core.Cpsr),
                (1, 0, 0b10) => ALU.ASR(core.R[rm], (byte)((shift >> 7) & 0b1_1111), ref core.Cpsr),
                (1, 0, 0b11) => ALU.ROR(core.R[rm], (byte)((shift >> 7) & 0b1_1111), ref core.Cpsr),

                (0, 1, 0b00) => ALU.LSLNoFlags(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111]),
                (0, 1, 0b01) => ALU.LSRNoFlags(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111]),
                (0, 1, 0b10) => ALU.ASRNoFlags(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111]),
                (0, 1, 0b11) => ALU.RORNoFlags(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111]),

                (1, 1, 0b00) => ALU.LSL(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111], ref core.Cpsr),
                (1, 1, 0b01) => ALU.LSR(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111], ref core.Cpsr),
                (1, 1, 0b10) => ALU.ASR(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111], ref core.Cpsr),
                (1, 1, 0b11) => ALU.ROR(core.R[rm], (byte)core.R[(shift >> 8) & 0b1111], ref core.Cpsr),
                _ => throw new Exception("Invalid data operation"),
            };

            // Shifts by a register quantity cause a single I cycle which we represent by adding a wait state
            // Note that I cycles aren't wait states, but I'm not convinced that you can tell from outside the cpu at the moment!
            if (useRegister == 1)
            {
                core.WaitStates++;
            }
        }

        switch (opcode)
        {
            case 0x0: // AND
                core.R[rd] = core.R[rn] & secondOperand;
                break;
            case 0x1: // EOR
                core.R[rd] = core.R[rn] ^ secondOperand;
                break;
            case 0x2: // SUB
                core.R[rd] = ALU.SUB(core.R[rn], secondOperand, ref core.Cpsr);
                break;
            case 0x3: // RSB
                core.R[rd] = ALU.SUB(secondOperand, core.R[rn], ref core.Cpsr);
                break;
            case 0x4: // ADD
                core.R[rd] = ALU.ADD(core.R[rn], secondOperand, ref core.Cpsr);
                break;
            case 0x5: // ADC
                core.R[rd] = ALU.ADC(core.R[rn], secondOperand, ref core.Cpsr);
                break;
            case 0x6: // SBC
                core.R[rd] = ALU.SBC(core.R[rn], secondOperand, ref core.Cpsr);
                break;
            case 0x7: // RSC
                core.R[rd] = ALU.SBC(secondOperand, core.R[rn], ref core.Cpsr);
                break;
            case 0x8: // TST
                {
                    var result = core.R[rn] & secondOperand;
                    ALU.SetZeroSignFlags(ref core.Cpsr, result);
                    return;
                }
            case 0x9: // TEQ
                {
                    var result = core.R[rn] ^ secondOperand;
                    ALU.SetZeroSignFlags(ref core.Cpsr, result);
                    return;
                }
            case 0xA: // CMP
                {
                    var result = ALU.SUB(core.R[rn], secondOperand, ref core.Cpsr);
                    ALU.SetZeroSignFlags(ref core.Cpsr, result);
                    return;
                }
            case 0xB: // CMN
                {
                    var result = ALU.ADD(core.R[rn], secondOperand, ref core.Cpsr);
                    ALU.SetZeroSignFlags(ref core.Cpsr, result);
                    return;
                }
            case 0xC: // ORR
                core.R[rd] = core.R[rn] | secondOperand;
                break;
            case 0xD: // MOV
                core.R[rd] = secondOperand;
                break;
            case 0xE: // BIC
                core.R[rd] = core.R[rn] & ~secondOperand;
                break;
            case 0xF: // MVN
                core.R[rd] = ~secondOperand;
                break;
            default:
                throw new Exception("Invalid data operation");
        }

        if (s == 1) ALU.SetZeroSignFlags(ref core.Cpsr, core.R[rd]);

        // Writes to PC cause 2 extra cycles as the pipeline is flushed, note
        // though that the 2 extra cycles aren't specific to the instruction
        // they're an artifact of the flushed pipeline so don't get treated
        // here as wait states
        if (rd == 15)
        {
            core.ClearPipeline();
        }
    }

    #region ALU Partials

    static partial void and_lli(Core core, uint instruction);
    static partial void and_llr(Core core, uint instruction);
    static partial void and_lri(Core core, uint instruction);
    static partial void and_lrr(Core core, uint instruction);
    static partial void and_ari(Core core, uint instruction);
    static partial void and_arr(Core core, uint instruction);
    static partial void and_rri(Core core, uint instruction);
    static partial void and_rrr(Core core, uint instruction);
    static partial void ands_lli(Core core, uint instruction);
    static partial void ands_llr(Core core, uint instruction);
    static partial void ands_lri(Core core, uint instruction);
    static partial void ands_lrr(Core core, uint instruction);
    static partial void ands_ari(Core core, uint instruction);
    static partial void ands_arr(Core core, uint instruction);
    static partial void ands_rri(Core core, uint instruction);
    static partial void ands_rrr(Core core, uint instruction);
    static partial void eor_lli(Core core, uint instruction);
    static partial void eor_llr(Core core, uint instruction);
    static partial void eor_lri(Core core, uint instruction);
    static partial void eor_lrr(Core core, uint instruction);
    static partial void eor_ari(Core core, uint instruction);
    static partial void eor_arr(Core core, uint instruction);
    static partial void eor_rri(Core core, uint instruction);
    static partial void eor_rrr(Core core, uint instruction);
    static partial void eors_lli(Core core, uint instruction);
    static partial void eors_llr(Core core, uint instruction);
    static partial void eors_lri(Core core, uint instruction);
    static partial void eors_lrr(Core core, uint instruction);
    static partial void eors_ari(Core core, uint instruction);
    static partial void eors_arr(Core core, uint instruction);
    static partial void eors_rri(Core core, uint instruction);
    static partial void eors_rrr(Core core, uint instruction);
    static partial void sub_lli(Core core, uint instruction);
    static partial void sub_llr(Core core, uint instruction);
    static partial void sub_lri(Core core, uint instruction);
    static partial void sub_lrr(Core core, uint instruction);
    static partial void sub_ari(Core core, uint instruction);
    static partial void sub_arr(Core core, uint instruction);
    static partial void sub_rri(Core core, uint instruction);
    static partial void sub_rrr(Core core, uint instruction);
    static partial void subs_lli(Core core, uint instruction);
    static partial void subs_llr(Core core, uint instruction);
    static partial void subs_lri(Core core, uint instruction);
    static partial void subs_lrr(Core core, uint instruction);
    static partial void subs_ari(Core core, uint instruction);
    static partial void subs_arr(Core core, uint instruction);
    static partial void subs_rri(Core core, uint instruction);
    static partial void subs_rrr(Core core, uint instruction);
    static partial void rsb_lli(Core core, uint instruction);
    static partial void rsb_llr(Core core, uint instruction);
    static partial void rsb_lri(Core core, uint instruction);
    static partial void rsb_lrr(Core core, uint instruction);
    static partial void rsb_ari(Core core, uint instruction);
    static partial void rsb_arr(Core core, uint instruction);
    static partial void rsb_rri(Core core, uint instruction);
    static partial void rsb_rrr(Core core, uint instruction);
    static partial void rsbs_lli(Core core, uint instruction);
    static partial void rsbs_llr(Core core, uint instruction);
    static partial void rsbs_lri(Core core, uint instruction);
    static partial void rsbs_lrr(Core core, uint instruction);
    static partial void rsbs_ari(Core core, uint instruction);
    static partial void rsbs_arr(Core core, uint instruction);
    static partial void rsbs_rri(Core core, uint instruction);
    static partial void rsbs_rrr(Core core, uint instruction);
    static partial void add_lli(Core core, uint instruction);
    static partial void add_llr(Core core, uint instruction);
    static partial void add_lri(Core core, uint instruction);
    static partial void add_lrr(Core core, uint instruction);
    static partial void add_ari(Core core, uint instruction);
    static partial void add_arr(Core core, uint instruction);
    static partial void add_rri(Core core, uint instruction);
    static partial void add_rrr(Core core, uint instruction);
    static partial void adds_lli(Core core, uint instruction);
    static partial void adds_llr(Core core, uint instruction);
    static partial void adds_lri(Core core, uint instruction);
    static partial void adds_lrr(Core core, uint instruction);
    static partial void adds_ari(Core core, uint instruction);
    static partial void adds_arr(Core core, uint instruction);
    static partial void adds_rri(Core core, uint instruction);
    static partial void adds_rrr(Core core, uint instruction);
    static partial void adc_lli(Core core, uint instruction);
    static partial void adc_llr(Core core, uint instruction);
    static partial void adc_lri(Core core, uint instruction);
    static partial void adc_lrr(Core core, uint instruction);
    static partial void adc_ari(Core core, uint instruction);
    static partial void adc_arr(Core core, uint instruction);
    static partial void adc_rri(Core core, uint instruction);
    static partial void adc_rrr(Core core, uint instruction);
    static partial void adcs_lli(Core core, uint instruction);
    static partial void adcs_llr(Core core, uint instruction);
    static partial void adcs_lri(Core core, uint instruction);
    static partial void adcs_lrr(Core core, uint instruction);
    static partial void adcs_ari(Core core, uint instruction);
    static partial void adcs_arr(Core core, uint instruction);
    static partial void adcs_rri(Core core, uint instruction);
    static partial void adcs_rrr(Core core, uint instruction);
    static partial void sbc_lli(Core core, uint instruction);
    static partial void sbc_llr(Core core, uint instruction);
    static partial void sbc_lri(Core core, uint instruction);
    static partial void sbc_lrr(Core core, uint instruction);
    static partial void sbc_ari(Core core, uint instruction);
    static partial void sbc_arr(Core core, uint instruction);
    static partial void sbc_rri(Core core, uint instruction);
    static partial void sbc_rrr(Core core, uint instruction);
    static partial void sbcs_lli(Core core, uint instruction);
    static partial void sbcs_llr(Core core, uint instruction);
    static partial void sbcs_lri(Core core, uint instruction);
    static partial void sbcs_lrr(Core core, uint instruction);
    static partial void sbcs_ari(Core core, uint instruction);
    static partial void sbcs_arr(Core core, uint instruction);
    static partial void sbcs_rri(Core core, uint instruction);
    static partial void sbcs_rrr(Core core, uint instruction);
    static partial void rsc_lli(Core core, uint instruction);
    static partial void rsc_llr(Core core, uint instruction);
    static partial void rsc_lri(Core core, uint instruction);
    static partial void rsc_lrr(Core core, uint instruction);
    static partial void rsc_ari(Core core, uint instruction);
    static partial void rsc_arr(Core core, uint instruction);
    static partial void rsc_rri(Core core, uint instruction);
    static partial void rsc_rrr(Core core, uint instruction);
    static partial void rscs_lli(Core core, uint instruction);
    static partial void rscs_llr(Core core, uint instruction);
    static partial void rscs_lri(Core core, uint instruction);
    static partial void rscs_lrr(Core core, uint instruction);
    static partial void rscs_ari(Core core, uint instruction);
    static partial void rscs_arr(Core core, uint instruction);
    static partial void rscs_rri(Core core, uint instruction);
    static partial void rscs_rrr(Core core, uint instruction);
    static partial void tsts_lli(Core core, uint instruction);
    static partial void tsts_llr(Core core, uint instruction);
    static partial void tsts_lri(Core core, uint instruction);
    static partial void tsts_lrr(Core core, uint instruction);
    static partial void tsts_ari(Core core, uint instruction);
    static partial void tsts_arr(Core core, uint instruction);
    static partial void tsts_rri(Core core, uint instruction);
    static partial void tsts_rrr(Core core, uint instruction);
    static partial void teqs_lli(Core core, uint instruction);
    static partial void teqs_llr(Core core, uint instruction);
    static partial void teqs_lri(Core core, uint instruction);
    static partial void teqs_lrr(Core core, uint instruction);
    static partial void teqs_ari(Core core, uint instruction);
    static partial void teqs_arr(Core core, uint instruction);
    static partial void teqs_rri(Core core, uint instruction);
    static partial void teqs_rrr(Core core, uint instruction);
    static partial void cmps_lli(Core core, uint instruction);
    static partial void cmps_llr(Core core, uint instruction);
    static partial void cmps_lri(Core core, uint instruction);
    static partial void cmps_lrr(Core core, uint instruction);
    static partial void cmps_ari(Core core, uint instruction);
    static partial void cmps_arr(Core core, uint instruction);
    static partial void cmps_rri(Core core, uint instruction);
    static partial void cmps_rrr(Core core, uint instruction);
    static partial void cmns_lli(Core core, uint instruction);
    static partial void cmns_llr(Core core, uint instruction);
    static partial void cmns_lri(Core core, uint instruction);
    static partial void cmns_lrr(Core core, uint instruction);
    static partial void cmns_ari(Core core, uint instruction);
    static partial void cmns_arr(Core core, uint instruction);
    static partial void cmns_rri(Core core, uint instruction);
    static partial void cmns_rrr(Core core, uint instruction);
    static partial void orr_lli(Core core, uint instruction);
    static partial void orr_llr(Core core, uint instruction);
    static partial void orr_lri(Core core, uint instruction);
    static partial void orr_lrr(Core core, uint instruction);
    static partial void orr_ari(Core core, uint instruction);
    static partial void orr_arr(Core core, uint instruction);
    static partial void orr_rri(Core core, uint instruction);
    static partial void orr_rrr(Core core, uint instruction);
    static partial void orrs_lli(Core core, uint instruction);
    static partial void orrs_llr(Core core, uint instruction);
    static partial void orrs_lri(Core core, uint instruction);
    static partial void orrs_lrr(Core core, uint instruction);
    static partial void orrs_ari(Core core, uint instruction);
    static partial void orrs_arr(Core core, uint instruction);
    static partial void orrs_rri(Core core, uint instruction);
    static partial void orrs_rrr(Core core, uint instruction);
    static partial void mov_lli(Core core, uint instruction);
    static partial void mov_llr(Core core, uint instruction);
    static partial void mov_lri(Core core, uint instruction);
    static partial void mov_lrr(Core core, uint instruction);
    static partial void mov_ari(Core core, uint instruction);
    static partial void mov_arr(Core core, uint instruction);
    static partial void mov_rri(Core core, uint instruction);
    static partial void mov_rrr(Core core, uint instruction);
    static partial void movs_lli(Core core, uint instruction);
    static partial void movs_llr(Core core, uint instruction);
    static partial void movs_lri(Core core, uint instruction);
    static partial void movs_lrr(Core core, uint instruction);
    static partial void movs_ari(Core core, uint instruction);
    static partial void movs_arr(Core core, uint instruction);
    static partial void movs_rri(Core core, uint instruction);
    static partial void movs_rrr(Core core, uint instruction);
    static partial void bic_lli(Core core, uint instruction);
    static partial void bic_llr(Core core, uint instruction);
    static partial void bic_lri(Core core, uint instruction);
    static partial void bic_lrr(Core core, uint instruction);
    static partial void bic_ari(Core core, uint instruction);
    static partial void bic_arr(Core core, uint instruction);
    static partial void bic_rri(Core core, uint instruction);
    static partial void bic_rrr(Core core, uint instruction);
    static partial void bics_lli(Core core, uint instruction);
    static partial void bics_llr(Core core, uint instruction);
    static partial void bics_lri(Core core, uint instruction);
    static partial void bics_lrr(Core core, uint instruction);
    static partial void bics_ari(Core core, uint instruction);
    static partial void bics_arr(Core core, uint instruction);
    static partial void bics_rri(Core core, uint instruction);
    static partial void bics_rrr(Core core, uint instruction);
    static partial void mvn_lli(Core core, uint instruction);
    static partial void mvn_llr(Core core, uint instruction);
    static partial void mvn_lri(Core core, uint instruction);
    static partial void mvn_lrr(Core core, uint instruction);
    static partial void mvn_ari(Core core, uint instruction);
    static partial void mvn_arr(Core core, uint instruction);
    static partial void mvn_rri(Core core, uint instruction);
    static partial void mvn_rrr(Core core, uint instruction);
    static partial void mvns_lli(Core core, uint instruction);
    static partial void mvns_llr(Core core, uint instruction);
    static partial void mvns_lri(Core core, uint instruction);
    static partial void mvns_lrr(Core core, uint instruction);
    static partial void mvns_ari(Core core, uint instruction);
    static partial void mvns_arr(Core core, uint instruction);
    static partial void mvns_rri(Core core, uint instruction);
    static partial void mvns_rrr(Core core, uint instruction);
    static partial void and_imm(Core core, uint instruction);
    static partial void ands_imm(Core core, uint instruction);
    static partial void eor_imm(Core core, uint instruction);
    static partial void eors_imm(Core core, uint instruction);
    static partial void sub_imm(Core core, uint instruction);
    static partial void subs_imm(Core core, uint instruction);
    static partial void rsb_imm(Core core, uint instruction);
    static partial void rsbs_imm(Core core, uint instruction);
    static partial void add_imm(Core core, uint instruction);
    static partial void adds_imm(Core core, uint instruction);
    static partial void adc_imm(Core core, uint instruction);
    static partial void adcs_imm(Core core, uint instruction);
    static partial void sbc_imm(Core core, uint instruction);
    static partial void sbcs_imm(Core core, uint instruction);
    static partial void rsc_imm(Core core, uint instruction);
    static partial void rscs_imm(Core core, uint instruction);
    static partial void tsts_imm(Core core, uint instruction);
    static partial void teqs_imm(Core core, uint instruction);
    static partial void cmps_imm(Core core, uint instruction);
    static partial void cmns_imm(Core core, uint instruction);
    static partial void orr_imm(Core core, uint instruction);
    static partial void orrs_imm(Core core, uint instruction);
    static partial void mov_imm(Core core, uint instruction);
    static partial void movs_imm(Core core, uint instruction);
    static partial void bic_imm(Core core, uint instruction);
    static partial void bics_imm(Core core, uint instruction);
    static partial void mvn_imm(Core core, uint instruction);
    static partial void mvns_imm(Core core, uint instruction);

    #endregion

    internal static void undefined(Core core, uint instruction) => throw new NotImplementedException("undefined not implemented");

    #region Multiply
    internal static void mul(Core core, uint instruction) => throw new NotImplementedException("mul not implemented");
    internal static void muls(Core core, uint instruction) => throw new NotImplementedException("muls not implemented");
    internal static void mla(Core core, uint instruction) => throw new NotImplementedException("mla not implemented");
    internal static void mlas(Core core, uint instruction) => throw new NotImplementedException("mlas not implemented");
    internal static void umull(Core core, uint instruction) => throw new NotImplementedException("umull not implemented");
    internal static void umulls(Core core, uint instruction) => throw new NotImplementedException("umulls not implemented");
    internal static void umlal(Core core, uint instruction) => throw new NotImplementedException("umlal not implemented");
    internal static void umlals(Core core, uint instruction) => throw new NotImplementedException("umlals not implemented");
    internal static void smull(Core core, uint instruction) => throw new NotImplementedException("smull not implemented");
    internal static void smulls(Core core, uint instruction) => throw new NotImplementedException("smulls not implemented");
    internal static void smlal(Core core, uint instruction) => throw new NotImplementedException("smlal not implemented");
    internal static void smlals(Core core, uint instruction) => throw new NotImplementedException("smlals not implemented");
    #endregion

    #region Half Word & Signed Single Data Transfers

    internal static void strh_ptrm(Core core, uint instruction) => throw new NotImplementedException("strh_ptrm not implemented");
    internal static void ldrh_ptrm(Core core, uint instruction) => throw new NotImplementedException("ldrh_ptrm not implemented");
    internal static void ldrsb_ptrm(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ptrm not implemented");
    internal static void ldrsh_ptrm(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ptrm not implemented");
    internal static void strh_ptim(Core core, uint instruction) => throw new NotImplementedException("strh_ptim not implemented");
    internal static void ldrh_ptim(Core core, uint instruction) => throw new NotImplementedException("ldrh_ptim not implemented");
    internal static void ldrsb_ptim(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ptim not implemented");
    internal static void ldrsh_ptim(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ptim not implemented");
    internal static void strh_ptrp(Core core, uint instruction) => throw new NotImplementedException("strh_ptrp not implemented");
    internal static void ldrh_ptrp(Core core, uint instruction) => throw new NotImplementedException("ldrh_ptrp not implemented");
    internal static void ldrsb_ptrp(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ptrp not implemented");
    internal static void ldrsh_ptrp(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ptrp not implemented");
    internal static void strh_ptip(Core core, uint instruction) => throw new NotImplementedException("strh_ptip not implemented");
    internal static void ldrh_ptip(Core core, uint instruction) => throw new NotImplementedException("ldrh_ptip not implemented");
    internal static void ldrsb_ptip(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ptip not implemented");
    internal static void ldrsh_ptip(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ptip not implemented");

    internal static void strh_ofrm(Core core, uint instruction) => throw new NotImplementedException("strh_ofrm not implemented");
    internal static void ldrh_ofrm(Core core, uint instruction) => throw new NotImplementedException("ldrh_ofrm not implemented");
    internal static void ldrsb_ofrm(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ofrm not implemented");
    internal static void ldrsh_ofrm(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ofrm not implemented");
    internal static void strh_prrm(Core core, uint instruction) => throw new NotImplementedException("strh_prrm not implemented");
    internal static void ldrh_prrm(Core core, uint instruction) => throw new NotImplementedException("ldrh_prrm not implemented");
    internal static void ldrsb_prrm(Core core, uint instruction) => throw new NotImplementedException("ldrsb_prrm not implemented");
    internal static void ldrsh_prrm(Core core, uint instruction) => throw new NotImplementedException("ldrsh_prrm not implemented");
    internal static void strh_ofim(Core core, uint instruction) => throw new NotImplementedException("strh_ofim not implemented");
    internal static void ldrh_ofim(Core core, uint instruction) => throw new NotImplementedException("ldrh_ofim not implemented");
    internal static void ldrsb_ofim(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ofim not implemented");
    internal static void ldrsh_ofim(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ofim not implemented");
    internal static void strh_prim(Core core, uint instruction) => throw new NotImplementedException("strh_prim not implemented");
    internal static void ldrh_prim(Core core, uint instruction) => throw new NotImplementedException("ldrh_prim not implemented");
    internal static void ldrsb_prim(Core core, uint instruction) => throw new NotImplementedException("ldrsb_prim not implemented");
    internal static void ldrsh_prim(Core core, uint instruction) => throw new NotImplementedException("ldrsh_prim not implemented");
    internal static void strh_ofrp(Core core, uint instruction) => throw new NotImplementedException("strh_ofrp not implemented");
    internal static void ldrh_ofrp(Core core, uint instruction) => throw new NotImplementedException("ldrh_ofrp not implemented");
    internal static void ldrsb_ofrp(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ofrp not implemented");
    internal static void ldrsh_ofrp(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ofrp not implemented");
    internal static void strh_prrp(Core core, uint instruction) => throw new NotImplementedException("strh_prrp not implemented");
    internal static void ldrh_prrp(Core core, uint instruction) => throw new NotImplementedException("ldrh_prrp not implemented");
    internal static void ldrsb_prrp(Core core, uint instruction) => throw new NotImplementedException("ldrsb_prrp not implemented");
    internal static void ldrsh_prrp(Core core, uint instruction) => throw new NotImplementedException("ldrsh_prrp not implemented");
    internal static void strh_ofip(Core core, uint instruction)
    {
        var offset = (instruction & 0b1111) | ((instruction >> 8) & 0b1111);
        var type = (instruction >> 5) & 0b11;
        var rd = (instruction >> 12) & 0b1111;
        var rn = (instruction >> 16) & 0b1111;

        var value = type switch
        {
            0b00 => throw new NotImplementedException("Think this should be a SWP not STRH"),
            0b01 => (ushort)core.R[rd],
            0b10 => (ushort)((sbyte)core.R[rd]),
            0b11 => (ushort)((short)core.R[rd]),
            _ => throw new Exception("Invalid state")
        };

        var address = core.R[rn] + offset;

        // Configure the memory unit for a write cycle 
        core.A = address;
        core.D = value;
        core.nOPC = true;
        core.nRW = true;
        core.SEQ = false;
        core.MAS = BusWidth.HalfWord;

        core.NextExecuteAction = &Core.ResetMemoryUnitForArmOpcodeFetch;
    }

    internal static void ldrh_ofip(Core core, uint instruction) => throw new NotImplementedException("ldrh_ofip not implemented");
    internal static void ldrsb_ofip(Core core, uint instruction) => throw new NotImplementedException("ldrsb_ofip not implemented");
    internal static void ldrsh_ofip(Core core, uint instruction) => throw new NotImplementedException("ldrsh_ofip not implemented");
    internal static void strh_prip(Core core, uint instruction) => throw new NotImplementedException("strh_prip not implemented");
    internal static void ldrh_prip(Core core, uint instruction) => throw new NotImplementedException("ldrh_prip not implemented");
    internal static void ldrsb_prip(Core core, uint instruction) => throw new NotImplementedException("ldrsb_prip not implemented");
    internal static void ldrsh_prip(Core core, uint instruction) => throw new NotImplementedException("ldrsh_prip not implemented");

    #endregion

    #region MSR/MRS
    internal static void mrs_rc(Core core, uint instruction)
    {
        var rd = (instruction >> 12) & 0b1111;
        core.R[rd] = core.Cpsr.Get();
        core.MoveExecutePipelineToNextInstruction();
    }
    internal static void mrs_rs(Core core, uint instruction)
    {
        var rd = (instruction >> 12) & 0b1111;
        core.R[rd] = core.CurrentSpsr().Get();
        core.MoveExecutePipelineToNextInstruction();
    }
    internal static void msr_rc(Core core, uint instruction)
    {
        var rm = instruction & 0b1111;
        // TODO - "In User mode, the control bits of the CPSR are protected from change, so only
        // the condition code flags of the CPSR can be changed. In other(privileged)
        // modes the entire CPSR can be changed"
        core.Cpsr.Set(core.R[rm]);
        core.MoveExecutePipelineToNextInstruction();
    }
    internal static void msr_rs(Core core, uint instruction)
    {
        // TODO - What does this do in user mode when there is no SPSR register?
        var rm = instruction & 0b1111;
        core.CurrentSpsr().Set(core.R[rm]);
        core.MoveExecutePipelineToNextInstruction();
    }
    internal static void msr_ic(Core core, uint instruction) => throw new NotImplementedException("msr_ic not implemented");
    internal static void msr_is(Core core, uint instruction) => throw new NotImplementedException("msr_is not implemented");
    #endregion

    #region SWP
    internal static void swp(Core core, uint instruction) => throw new NotImplementedException("swp not implemented");
    internal static void swpb(Core core, uint instruction) => throw new NotImplementedException("swpb not implemented");
    #endregion SWP

    #region Single Data Transfer partials
    static partial void str_ptim(Core core, uint instruction);
    static partial void ldr_ptim(Core core, uint instruction);
    static partial void strb_ptim(Core core, uint instruction);
    static partial void ldrb_ptim(Core core, uint instruction);
    static partial void str_ptip(Core core, uint instruction);
    static partial void ldr_ptip(Core core, uint instruction);
    static partial void strb_ptip(Core core, uint instruction);
    static partial void ldrb_ptip(Core core, uint instruction);
    static partial void str_ofim(Core core, uint instruction);
    static partial void ldr_ofim(Core core, uint instruction);
    static partial void str_prim(Core core, uint instruction);
    static partial void ldr_prim(Core core, uint instruction);
    static partial void strb_ofim(Core core, uint instruction);
    static partial void ldrb_ofim(Core core, uint instruction);
    static partial void strb_prim(Core core, uint instruction);
    static partial void ldrb_prim(Core core, uint instruction);
    static partial void str_ofip(Core core, uint instruction);
    static partial void ldr_ofip(Core core, uint instruction);
    static partial void str_prip(Core core, uint instruction);
    static partial void ldr_prip(Core core, uint instruction);
    static partial void strb_ofip(Core core, uint instruction);
    static partial void ldrb_ofip(Core core, uint instruction);
    static partial void strb_prip(Core core, uint instruction);
    static partial void ldrb_prip(Core core, uint instruction);
    static partial void str_ptrmll(Core core, uint instruction);
    static partial void str_ptrmlr(Core core, uint instruction);
    static partial void str_ptrmar(Core core, uint instruction);
    static partial void str_ptrmrr(Core core, uint instruction);
    static partial void ldr_ptrmll(Core core, uint instruction);
    static partial void ldr_ptrmlr(Core core, uint instruction);
    static partial void ldr_ptrmar(Core core, uint instruction);
    static partial void ldr_ptrmrr(Core core, uint instruction);
    static partial void strb_ptrmll(Core core, uint instruction);
    static partial void strb_ptrmlr(Core core, uint instruction);
    static partial void strb_ptrmar(Core core, uint instruction);
    static partial void strb_ptrmrr(Core core, uint instruction);
    static partial void ldrb_ptrmll(Core core, uint instruction);
    static partial void ldrb_ptrmlr(Core core, uint instruction);
    static partial void ldrb_ptrmar(Core core, uint instruction);
    static partial void ldrb_ptrmrr(Core core, uint instruction);
    static partial void str_ptrpll(Core core, uint instruction);
    static partial void str_ptrplr(Core core, uint instruction);
    static partial void str_ptrpar(Core core, uint instruction);
    static partial void str_ptrprr(Core core, uint instruction);
    static partial void ldr_ptrpll(Core core, uint instruction);
    static partial void ldr_ptrplr(Core core, uint instruction);
    static partial void ldr_ptrpar(Core core, uint instruction);
    static partial void ldr_ptrprr(Core core, uint instruction);
    static partial void strb_ptrpll(Core core, uint instruction);
    static partial void strb_ptrplr(Core core, uint instruction);
    static partial void strb_ptrpar(Core core, uint instruction);
    static partial void strb_ptrprr(Core core, uint instruction);
    static partial void ldrb_ptrpll(Core core, uint instruction);
    static partial void ldrb_ptrplr(Core core, uint instruction);
    static partial void ldrb_ptrpar(Core core, uint instruction);
    static partial void ldrb_ptrprr(Core core, uint instruction);
    static partial void str_ofrmll(Core core, uint instruction);
    static partial void str_ofrmlr(Core core, uint instruction);
    static partial void str_ofrmar(Core core, uint instruction);
    static partial void str_ofrmrr(Core core, uint instruction);
    static partial void ldr_ofrmll(Core core, uint instruction);
    static partial void ldr_ofrmlr(Core core, uint instruction);
    static partial void ldr_ofrmar(Core core, uint instruction);
    static partial void ldr_ofrmrr(Core core, uint instruction);
    static partial void str_prrmll(Core core, uint instruction);
    static partial void str_prrmlr(Core core, uint instruction);
    static partial void str_prrmar(Core core, uint instruction);
    static partial void str_prrmrr(Core core, uint instruction);
    static partial void ldr_prrmll(Core core, uint instruction);
    static partial void ldr_prrmlr(Core core, uint instruction);
    static partial void ldr_prrmar(Core core, uint instruction);
    static partial void ldr_prrmrr(Core core, uint instruction);
    static partial void strb_ofrmll(Core core, uint instruction);
    static partial void strb_ofrmlr(Core core, uint instruction);
    static partial void strb_ofrmar(Core core, uint instruction);
    static partial void strb_ofrmrr(Core core, uint instruction);
    static partial void ldrb_ofrmll(Core core, uint instruction);
    static partial void ldrb_ofrmlr(Core core, uint instruction);
    static partial void ldrb_ofrmar(Core core, uint instruction);
    static partial void ldrb_ofrmrr(Core core, uint instruction);
    static partial void strb_prrmll(Core core, uint instruction);
    static partial void strb_prrmlr(Core core, uint instruction);
    static partial void strb_prrmar(Core core, uint instruction);
    static partial void strb_prrmrr(Core core, uint instruction);
    static partial void ldrb_prrmll(Core core, uint instruction);
    static partial void ldrb_prrmlr(Core core, uint instruction);
    static partial void ldrb_prrmar(Core core, uint instruction);
    static partial void ldrb_prrmrr(Core core, uint instruction);
    static partial void str_ofrpll(Core core, uint instruction);
    static partial void str_ofrplr(Core core, uint instruction);
    static partial void str_ofrpar(Core core, uint instruction);
    static partial void str_ofrprr(Core core, uint instruction);
    static partial void ldr_ofrpll(Core core, uint instruction);
    static partial void ldr_ofrplr(Core core, uint instruction);
    static partial void ldr_ofrpar(Core core, uint instruction);
    static partial void ldr_ofrprr(Core core, uint instruction);
    static partial void str_prrpll(Core core, uint instruction);
    static partial void str_prrplr(Core core, uint instruction);
    static partial void str_prrpar(Core core, uint instruction);
    static partial void str_prrprr(Core core, uint instruction);
    static partial void ldr_prrpll(Core core, uint instruction);
    static partial void ldr_prrplr(Core core, uint instruction);
    static partial void ldr_prrpar(Core core, uint instruction);
    static partial void ldr_prrprr(Core core, uint instruction);
    static partial void strb_ofrpll(Core core, uint instruction);
    static partial void strb_ofrplr(Core core, uint instruction);
    static partial void strb_ofrpar(Core core, uint instruction);
    static partial void strb_ofrprr(Core core, uint instruction);
    static partial void ldrb_ofrpll(Core core, uint instruction);
    static partial void ldrb_ofrplr(Core core, uint instruction);
    static partial void ldrb_ofrpar(Core core, uint instruction);
    static partial void ldrb_ofrprr(Core core, uint instruction);
    static partial void strb_prrpll(Core core, uint instruction);
    static partial void strb_prrplr(Core core, uint instruction);
    static partial void strb_prrpar(Core core, uint instruction);
    static partial void strb_prrprr(Core core, uint instruction);
    static partial void ldrb_prrpll(Core core, uint instruction);
    static partial void ldrb_prrplr(Core core, uint instruction);
    static partial void ldrb_prrpar(Core core, uint instruction);
    static partial void ldrb_prrprr(Core core, uint instruction);

    #endregion

    #region Store Multiple Registers
    /// <summary>
    /// Whilst a STM/LDM operation is going on we need to track which register
    /// is going to be stored/loaded on the next cycle.
    /// 
    /// In other words, this is nasty global state about how far through a 
    /// stm/ldm instruction we've got.
    /// </summary>
    private static uint[] _storeLoadMultipleState = new uint[16];
    private static int _storeLoadMultiplePopCount;
    private static int _storeLoadMultiplePtr;
    private static uint _storeLoadMutipleFinalWritebackValue;
    private static bool _storeLoadMultipleDoWriteback;

    static partial void stmda(Core core, uint instruction);
    static partial void stmda_w(Core core, uint instruction);
    static partial void stmda_u(Core core, uint instruction);
    static partial void stmda_uw(Core core, uint instruction);
    static partial void stmia(Core core, uint instruction);
    static partial void stmia_w(Core core, uint instruction);
    static partial void stmia_u(Core core, uint instruction);
    static partial void stmia_uw(Core core, uint instruction);
    static partial void stmdb(Core core, uint instruction);
    static partial void stmdb_w(Core core, uint instruction);
    static partial void stmdb_u(Core core, uint instruction);
    static partial void stmdb_uw(Core core, uint instruction);
    static partial void stmib(Core core, uint instruction);
    static partial void stmib_w(Core core, uint instruction);
    static partial void stmib_u(Core core, uint instruction);
    static partial void stmib_uw(Core core, uint instruction);

    internal static void stm_registerWriteCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount)
        {
            if (_storeLoadMultipleDoWriteback)
            {
                var rn = (instruction >> 16) & 0b1111;
                core.R[rn] = _storeLoadMutipleFinalWritebackValue;
                if (rn == 15) core.ClearPipeline(); // TODO - This is a really bad idea, why would you use PC as writeback, probably needs a special log
            }

            Core.ResetMemoryUnitForArmOpcodeFetch(core, instruction);
        }
        else
        {
            core.A += 4;
            core.D = _storeLoadMultipleState[_storeLoadMultiplePtr];
            _storeLoadMultiplePtr++;
        }
    }

    internal static void ldm_registerReadCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount - 1)
        {
            if (_storeLoadMultipleDoWriteback)
            {
                var rn = (instruction >> 16) & 0b1111;
                core.R[rn] = _storeLoadMutipleFinalWritebackValue;
                if (rn == 15) core.ClearPipeline();
            }

            Core.ResetMemoryUnitForArmOpcodeFetch(core, instruction);
        }
        else
        {
            core.A += 4;
            core.R[_storeLoadMultipleState[_storeLoadMultiplePtr]] = core.D;
            if (_storeLoadMultipleState[_storeLoadMultiplePtr] == 15) core.ClearPipeline();
            _storeLoadMultiplePtr++;
        }
    }
    #endregion

    #region Load Multiple Registers
    static partial void ldmda(Core core, uint instruction);
    static partial void ldmda_w(Core core, uint instruction);
    static partial void ldmda_u(Core core, uint instruction);
    static partial void ldmda_uw(Core core, uint instruction);
    static partial void ldmia(Core core, uint instruction);
    static partial void ldmia_w(Core core, uint instruction);
    static partial void ldmia_u(Core core, uint instruction);
    static partial void ldmia_uw(Core core, uint instruction);
    static partial void ldmdb(Core core, uint instruction);
    static partial void ldmdb_w(Core core, uint instruction);
    static partial void ldmdb_u(Core core, uint instruction);
    static partial void ldmdb_uw(Core core, uint instruction);
    static partial void ldmib(Core core, uint instruction);
    static partial void ldmib_w(Core core, uint instruction);
    static partial void ldmib_u(Core core, uint instruction);
    static partial void ldmib_uw(Core core, uint instruction);

    #endregion

    internal static void b(Core core, uint instruction)
    {
        var offset = ((int)((instruction & 0xFF_FFFF) << 8)) >> 6;
        core.R[15] = (uint)(core.R[15] + offset);
        core.A = core.R[15];
        core.ClearPipeline(); // Note that this will trigger two more cycles (both just fetches, with nothing to execute)

        core.MoveExecutePipelineToNextInstruction();
    }

    private static uint BlReturnAddress;
    internal static void bl(Core core, uint instruction)
    {
        BlReturnAddress = core.R[15] - 4;
        b(core, instruction);
        core.NextExecuteAction = &bl_2;
    }

    internal static void bl_2(Core core, uint instruction)
    {
        core.R[14] = BlReturnAddress;
        core.MoveExecutePipelineToNextInstruction();
    }

    internal static void bx(Core core, uint instruction)
    {
        var rn = instruction & 0b1111;
        core.R[15] = (uint)(core.R[rn] & (~1));
        core.A = core.R[15];
        core.ClearPipeline();

        // "When the instruction is executed, the value of Rn[0] determines whether
        // the instruction stream will be decoded as ARM or THUMB instructions"
        if ((core.R[rn] & 0b1) == 1)
        {
            core.SwitchToThumb();
        }
        else
        {
            core.SwitchToArm();
        }

        core.MoveExecutePipelineToNextInstruction();
    }

    internal static void stc_ofm(Core core, uint instruction) => throw new NotImplementedException("stc_ofm not implemented");
    internal static void ldc_ofm(Core core, uint instruction) => throw new NotImplementedException("ldc_ofm not implemented");
    internal static void stc_prm(Core core, uint instruction) => throw new NotImplementedException("stc_prm not implemented");
    internal static void ldc_prm(Core core, uint instruction) => throw new NotImplementedException("ldc_prm not implemented");
    internal static void stc_ofp(Core core, uint instruction) => throw new NotImplementedException("stc_ofp not implemented");
    internal static void ldc_ofp(Core core, uint instruction) => throw new NotImplementedException("ldc_ofp not implemented");
    internal static void stc_prp(Core core, uint instruction) => throw new NotImplementedException("stc_prp not implemented");
    internal static void ldc_prp(Core core, uint instruction) => throw new NotImplementedException("ldc_prp not implemented");
    internal static void stc_unm(Core core, uint instruction) => throw new NotImplementedException("stc_unm not implemented");
    internal static void ldc_unm(Core core, uint instruction) => throw new NotImplementedException("ldc_unm not implemented");
    internal static void stc_ptm(Core core, uint instruction) => throw new NotImplementedException("stc_ptm not implemented");
    internal static void ldc_ptm(Core core, uint instruction) => throw new NotImplementedException("ldc_ptm not implemented");
    internal static void stc_unp(Core core, uint instruction) => throw new NotImplementedException("stc_unp not implemented");
    internal static void ldc_unp(Core core, uint instruction) => throw new NotImplementedException("ldc_unp not implemented");
    internal static void stc_ptp(Core core, uint instruction) => throw new NotImplementedException("stc_ptp not implemented");
    internal static void ldc_ptp(Core core, uint instruction) => throw new NotImplementedException("ldc_ptp not implemented");
    internal static void cdp(Core core, uint instruction) => throw new NotImplementedException("cdp not implemented");
    internal static void mcr(Core core, uint instruction) => throw new NotImplementedException("mcr not implemented");
    internal static void mrc(Core core, uint instruction) => throw new NotImplementedException("mrc not implemented");
    internal static void swi(Core core, uint instruction) => throw new NotImplementedException("swi not implemented");

#pragma warning restore IDE1006 // Naming Styles
}
