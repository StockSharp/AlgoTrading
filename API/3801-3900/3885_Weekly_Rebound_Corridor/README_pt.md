# Estratégia semanal do corredor de recuperação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Weekly Rebound Corridor replica o comportamento do MetaTrader 4 Expert Advisor `2_Otkat_Sys_v1_1`. O sistema procura uma forte lacuna entre o fechamento da sessão anterior e o preço de abertura que ocorreu 24 velas antes. Quando o gap detectado excede um limite de corredor configurável e é o dia de negociação da semana especificado, a estratégia entra no mercado durante os primeiros minutos do novo dia de negociação. São aplicados níveis protetores de stop-loss e take-profit, e todas as posições abertas são fechadas à força pouco antes do término da sessão de negociação.

## Lógica de negociação
1. **Preparação de dados**
   - Usa velas de minutos por padrão. O tipo de vela é configurável para acomodar outros tamanhos de barra.
   - Acompanha o fechamento da vela anterior e mantém um buffer circular que retorna o preço de abertura observado há 24 velas.
2. **Geração de sinal**
   - No dia de negociação da semana especificado (formato MetaTrader: `0 = Sunday`, `6 = Saturday`), a estratégia avalia velas concluídas cujo horário local está entre 00h00 e 00h03.
   - Calcula a diferença entre a abertura histórica (24 velas atrás) e a última vela fechada. Se a diferença exceder o limite do corredor configurado, uma ordem de mercado será enviada:
     - **Configuração longa**: a abertura histórica menos o fechamento anterior é maior que o limite do corredor.
     - **Configuração curta**: o fechamento anterior menos a abertura histórica é maior que o limite do corredor.
   - Cada dia de negociação pode acionar no máximo uma entrada.
3. **Gestão Comercial**
   - Os níveis de stop-loss e take-profit são expressos em pontos. O tamanho do tick do instrumento converte os valores dos pontos em compensações de preços reais.
   - As negociações longas adicionam o deslocamento MT4 original de três pontos extras à distância de obtenção de lucro.
   - A estratégia monitora continuamente os máximos e mínimos das velas para detectar acertos de stop-loss ou take-profit e fecha a posição aberta com uma ordem de mercado quando acionada.
   - Qualquer posição aberta restante será fechada após as 22h45, horário local do câmbio, para emular a regra fixa de final do dia do Expert Advisor original.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `TakeProfitPoints` | Distância de lucro em pontos. As negociações longas adicionam três pontos adicionais, conforme definido no script MT4. | `5` |
| `StopLossPoints` | Distância de stop-loss em pontos. | `49` |
| `TradeVolume` | Volume submetido com ordens de mercado. O valor é alinhado automaticamente com o passo de volume do instrumento. | `1` |
| `CorridorPoints` | Gap mínimo exigido entre a abertura histórica e o fechamento mais recente. | `10` |
| `TradeDayOfWeek` | Dia de negociação em numeração MetaTrader (`0 = Sunday`… `6 = Saturday`). | `5` (sexta-feira) |
| `CandleType` | Tipo de dados Candle usado para análise. | `1 minute` |

## Notas
- A estratégia opera exclusivamente em velas concluídas para alinhar com as diretrizes do projeto.
- Certifique-se de que o instrumento selecionado forneça dados históricos suficientes para construir o buffer de 24 velas antes de esperar entradas.
- Os parâmetros baseados em volume e pontos devem ser ajustados para corresponder à especificação do instrumento (tamanho do tick, passo do lote, cronograma de negociação).
