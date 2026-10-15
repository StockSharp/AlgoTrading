# Estratégia Especialista ADC PL Stoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Expert ADC PL Stoch** é uma estratégia de padrão de velas convertida do MQL5 consultor especialista original *Expert_ADC_PL_Stoch*. Ele procura formações de Piercing Line de alta e Dark Cloud Cover de baixa em velas finalizadas e confirma os sinais com a linha %D de um oscilador Stochastic. The method is trend-following when the market retraces into an established move and requires the oscillator to be in extreme zones before opening positions. Position exits are based on Stochastic crossovers out of extreme areas, mirroring the vote-based exit logic of the source system.

## Lógica de negociação

1. Assine um tipo de vela configurável (padrão: período de 1 hora).
2. Para cada vela finalizada, mantenha as últimas velas necessárias para avaliação do padrão de velas e os valores recentes de Stochastic %D.
3. **Long Entry**
   - O par de velas anterior deve formar um padrão Piercing Line:
     - A vela na barra *t-1* é de alta com um corpo maior que o tamanho médio do corpo.
     - A vela na barra *t-2* é de baixa com um corpo maior que a média.
     - A vela de alta fica abaixo da mínima de baixa e fecha dentro do corpo de baixa, enquanto a tendência geral é de queda de acordo com a média próxima.
   - O valor Stochastic %D na barra *t-1* deve estar abaixo do limite de entrada longo (padrão 30).
4. **Short Entry**
   - O par de velas anterior deve formar um padrão Dark Cloud Cover:
     - A vela na barra *t-2* é de alta com um corpo grande.
     - A vela na barra *t-1* abre acima da máxima anterior e fecha dentro do corpo de alta.
     - O preço médio da vela de baixa está acima da média móvel de fechamento, sinalizando uma tendência de alta antes da reversão.
   - The Stochastic %D on bar *t-1* must be above the short entry threshold (default 70).
5. **Exit Conditions**
   - Long positions are closed when the Stochastic %D on bar *t-1* crosses below either the upper (80) or lower (20) thresholds compared with bar *t-2*.
   - As posições curtas são fechadas quando Stochastic %D na barra *t-1* ultrapassa os limites inferior (20) ou superior (80) em comparação com a barra *t-2*.
6. Todos os cálculos são realizados em velas prontas; nenhum processamento intrabar é usado.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Período de tempo das velas usadas para detecção de padrões. | 1 hora |
| `StochasticLength` | Comprimento base para o oscilador Stochastic. | 47 |
| `StochasticKPeriod` | Suavização do comprimento da linha %K. | 9 |
| `StochasticDPeriod` | Suavização do comprimento da linha %D. | 13 |
| `StochasticSlow` | Additional slowing factor applied to the oscillator. | 3 |
| `AverageBodyPeriod` | Número de velas usadas para medir o tamanho do corpo de referência e fechar a média. | 5 |
| `LongEntryThreshold` | Valor máximo de %D permitido antes de entrar em negociações longas. | 30 |
| `ShortEntryThreshold` | Valor mínimo de %D exigido antes de entrar em negociações curtas. | 70 |
| `ExitLowerThreshold` | Limite inferior usado para cruzamentos de saída. | 20 |
| `ExitUpperThreshold` | Upper boundary used for exit crossovers. | 80 |

## Gestão de risco

- A estratégia envia ordens de mercado utilizando o volume da estratégia base (padrão 1 contrato/lote).
- Nenhuma ordem de proteção automática está configurada; gerenciamento de risco externo ou `StartProtection` pode ser adicionado, se necessário.
- Apenas uma posição é gerenciada por vez; sinais opostos fecham a posição ativa antes de abrir uma nova.

## Notas

- Os corpos médios das velas e as médias próximas são calculadas a partir de velas históricas para replicar de perto a lógica de voto MQL5.
- Os valores Stochastic são armazenados por barra finalizada para avaliar os mesmos deslocamentos usados no consultor especialista original.
- As negociações são abertas e fechadas somente quando a estratégia está totalmente formada e a negociação é permitida pelas verificações da classe base.
