# Estratégia de Rompimento da Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o clássico consultor especializado "BreakOut" do MetaTrader de Soubra2003. Ela monitora a máxima e a
mínima da vela completada mais recente e reage sempre que o fechamento atual rompe esses níveis de referência. A abordagem é
totalmente simétrica: posições compradas são abertas em rompimentos altistas, e posições vendidas são abertas em rompimentos
baixistas. Buffers opcionais de stop-loss e take-profit expressos em unidades de preço permitem ao usuário limitar o risco ou
bloquear ganhos.

## Visão Geral

- Assina uma única série de velas (período de 1 hora por padrão).
- Armazena a máxima e a mínima da vela anterior para agir como gatilhos de rompimento.
- Opera apenas no fechamento da vela para espelhar a lógica original baseada em ticks sem depender de dados intrabar.
- Suporta tanto trades comprados quanto vendidos e sempre permanece zerado quando nenhuma condição de rompimento está ativa.

## Regras de Trading

1. **Entrada por rompimento / reversão**
   - Quando o fechamento da vela atual concluída está estritamente acima da máxima da vela anterior:
     - Qualquer posição vendida aberta é fechada a mercado.
     - Uma nova posição comprada é aberta imediatamente depois (a reversão acontece dentro do mesmo passo de processamento da vela).
   - Quando o fechamento está estritamente abaixo da mínima da vela anterior:
     - Qualquer posição comprada aberta é fechada a mercado.
     - Uma nova posição vendida é aberta depois.
2. **Saídas de proteção (opcional)**
   - Se um offset de stop-loss for configurado (> 0), a estratégia sai de uma posição comprada quando o fechamento cai `offset`
     unidades abaixo do preço de entrada, ou sai de uma posição vendida quando o fechamento sobe `offset` unidades acima do
     preço de entrada.
   - Se um offset de take-profit for configurado (> 0), a estratégia sai de uma posição comprada quando o fechamento sobe
     `offset` unidades acima do preço de entrada, ou sai de uma posição vendida quando o fechamento cai `offset` unidades
     abaixo do preço de entrada.
3. **Reinicialização do estado**
   - Após cada vela ser processada, a máxima e a mínima mais recentes se tornam os novos níveis de referência de rompimento.

## Parâmetros

- **Candle Type** – tipo de dados usado para assinatura (padrão para período horário). Defina como o tamanho de barra que
  corresponde ao gráfico usado no MetaTrader para o consultor especializado original.
- **Stop Loss** – distância em unidades de preço absoluto entre o preço de entrada e o stop de proteção. Mantenha em `0` para
  desabilitar o tratamento de stop-loss.
- **Take Profit** – distância em unidades de preço absoluto entre o preço de entrada e o alvo de lucro. Mantenha em `0` para
  desabilitar o tratamento de take-profit.

## Notas

- Os cálculos de stop-loss e take-profit são realizados em preços de fechamento de velas. A versão MQL4 original anexava níveis
  estáticos de SL/TP às ordens; no StockSharp as saídas são simuladas enviando ordens a mercado assim que os limiares são atingidos.
- Use incrementos de preço específicos do instrumento ao configurar os offsets. Por exemplo, se o instrumento opera com tamanho
  de tick de 0.01 e você quer um stop de 20 ticks, defina o parâmetro de stop-loss como `0.20`.
- Como a lógica sempre faz referência à vela imediatamente anterior, a estratégia funciona melhor em instrumentos em tendência
  ou durante sessões de alta volatilidade onde os rompimentos são significativos.

## Origem

- **Fonte**: `MQL/17306/BreakOut.mq4` (consultor especializado BreakOut de Soubra2003)
- **Autor**: https://www.mql5.com/en/users/soubra2003
