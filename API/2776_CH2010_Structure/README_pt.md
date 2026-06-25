# Estratégia CH2010 Structure de Rompimento Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o comportamento do especialista original **ch2010structure.mq5** rastreando múltiplos pares forex em dois períodos. Cada instrumento monitora a vela diária para determinar um viés direcional e depois observa velas de 30 minutos em busca de rompimentos além do intervalo diário anterior. Posições de mercado são abertas quando o rompimento se alinha com a tendência diária e fechadas usando níveis protetores de stop-loss e take-profit.

## Lógica principal

1. **Detecção de viés diário**  
   * A estratégia subscreve às velas diárias para USDCHF, GBPUSD, AUDUSD, USDJPY e EURGBP.  
   * Quando uma vela diária termina, a relação fechamento/abertura define o viés: de alta, de baixa ou neutro.  
   * O máximo, mínimo e fechamento diário são armazenados junto com a data da sessão para que a lógica intradia possa confirmar que está operando a mesma sessão.

2. **Execução de rompimentos intradia**  
   * As velas de 30 minutos são avaliadas assim que fecham.  
   * Se o fechamento estiver acima do máximo diário anterior mais um buffer configurável e o viés não for de baixa, um trade comprado é acionado.  
   * Se o fechamento estiver abaixo do mínimo diário anterior menos o buffer e o viés não for de alta, um trade vendido é acionado.  
   * Apenas um rompimento comprado e um vendido pode ser ativado por instrumento a cada dia para evitar operar excessivamente.

3. **Gerenciamento de risco inspirado nas funções helper originais**  
   * Os volumes são limitados entre `MinTradeVolume` e `MaxTradeVolume` e a posição agregada em todos os instrumentos é restringida por `MaxAggregateVolume`.  
   * Cada posição preenchida calcula imediatamente os níveis absolutos de stop-loss e take-profit usando offsets percentuais a partir do preço de entrada.  
   * As posições são fechadas por ordens de mercado assim que o stop ou alvo é atingido; ordens de saída repetidas são evitadas pelo flag `ExitInProgress`.

4. **Rastreamento de estado**  
   * Para cada instrumento, a estratégia rastreia seus próprios níveis diários, última posição conhecida, lado de entrada, ordens de saída e flags de rompimento em um `InstrumentContext`.  
   * Isso permite o fluxo de trabalho multi-símbolo sem ter que manter coleções personalizadas fora da classe de contexto.

## Parâmetros da estratégia

| Parâmetro | Descrição |
| --- | --- |
| `TradeVolume` | Volume base usado para novas entradas, sujeito aos limites de volume. |
| `MinTradeVolume` e `MaxTradeVolume` | Limites que espelham o filtro de risco original do MQL. |
| `MaxAggregateVolume` | Soma máxima de posições absolutas em todos os pares operados. |
| `StopLossPercent` | Offset do stop de proteção em porcentagem a partir do preço de entrada detectado. |
| `TakeProfitPercent` | Offset do take-profit em porcentagem a partir do preço de entrada detectado. |
| `BreakoutBufferPercent` | Porcentagem do intervalo diário anterior adicionada aos gatilhos de rompimento. |
| `DailyCandleType` | DataType usado para solicitar as velas de período superior. |
| `IntradayCandleType` | DataType usado para solicitar as velas de período de execução. |
| `UsdChfSecurity` .. `EurGbpSecurity` | Objetos de instrumento para os cinco símbolos forex monitorados por padrão. |

## Dados necessários

* Velas diárias para cada símbolo configurado (padrão: período de 1 dia).  
* Velas intradia (padrão: 30 minutos) para os mesmos símbolos.  
* Roteamento de ordens em tempo real para enviar ordens de mercado para cada instrumento.

## Notas de uso

1. Configurar os cinco parâmetros de instrumento antes de iniciar a estratégia. Podem ser substituídos por outros instrumentos se desejado.  
2. Definir o portfólio e conector como em outras estratégias StockSharp.  
3. Opcionalmente ajustar o buffer de rompimento ou parâmetros de risco para refletir as especificações de contrato do broker alvo.  
4. Iniciar a estratégia. Ela subscreverá automaticamente a ambos os fluxos de velas para cada instrumento, registrará a estrutura diária e aguardará rompimentos intradia válidos.  
5. Monitorar o log para entradas como `Daily candle captured` e `Enter Buy` para verificar o fluxo de decisões.

## Diferenças vs. o Especialista MQL original

* Ordens pendentes são substituídas por ordens de mercado imediatas assim que a condição de rompimento é observada. Isso mantém a lógica compatível com a API de alto nível do StockSharp enquanto preserva a ideia de limitar a exposição e reagir apenas uma vez por direção a cada dia.  
* As restrições de volume do helper `DebugOrderSend` foram adaptadas em parâmetros que limitam os tamanhos de trades individuais e a exposição total.  
* Registro extensivo é adicionado para mostrar níveis diários, razões de entrada e gatilhos de saída em comentários em inglês para facilitar a depuração no StockSharp.

## Isenção de responsabilidade

Este exemplo é destinado a propósitos educacionais. Parâmetros e instrumentos devem ser revisados e ajustados antes de usar a estratégia em trading de produção.
