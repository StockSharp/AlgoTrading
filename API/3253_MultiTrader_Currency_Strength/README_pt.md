# Estratégia de MultiTrader Currency Strength (3253)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um porte de alto nível StockSharp do painel público "MultiTrader" de MQL (base de código #24786). O Consultor Especialista original era um painel discricionário que exibia a força relativa das oito principais moedas, acionava alertas visuais/sonoros quando uma moeda se tornava extremamente forte ou fraca, e sugeria qual par de Forex negociar. A versão StockSharp automatiza o mesmo fluxo de trabalho analítico e opcionalmente executa trades no par mais forte vs. mais fraco.

A lógica calcula uma posição percentual do fechamento de cada símbolo dentro de sua faixa de candle atual. A média dos cruzamentos relevantes produz uma pontuação de força para AUD, CAD, CHF, EUR, GBP, JPY, NZD e USD. Quando uma moeda sobe acima do limiar de compra configurável e outra cai abaixo do limiar de venda, a estratégia recomenda o par construído a partir dessas moedas. Se o par existir no universo configurado, a estratégia pode colocar automaticamente uma ordem de mercado nessa direção.

## Modelo de força de moeda
A pontuação percentual de um símbolo é calculada como:

```
percent = 100 * (Close - Low) / (High - Low)
```

A força de cada moeda é derivada de sete cruzamentos, espelhando a implementação MQL. Uma inversão `100 - percent` é aplicada quando a moeda aparece como moeda de cotação no par:

| Moeda | Componentes |
| --- | --- |
| AUD | AUDJPY, AUDNZD, AUDUSD, 100-EURAUD, 100-GBPAUD, AUDCHF, AUDCAD |
| CAD | CADJPY, 100-NZDCAD, 100-USDCAD, 100-EURCAD, 100-GBPCAD, 100-AUDCAD, CADCHF |
| CHF | CHFJPY, 100-NZDCHF, 100-USDCHF, 100-EURCHF, 100-GBPCHF, 100-AUDCHF, 100-CADCHF |
| EUR | EURJPY, EURNZD, EURUSD, EURCAD, EURGBP, EURAUD, EURCHF |
| GBP | GBPJPY, GBPNZD, GBPUSD, GBPCAD, 100-EURGBP, GBPAUD, GBPCHF |
| JPY | 100-AUDJPY, 100-CHFJPY, 100-CADJPY, 100-EURJPY, 100-GBPJPY, 100-NZDJPY, 100-USDJPY |
| NZD | NZDJPY, 100-GBPNZD, NZDUSD, NZDCAD, 100-EURNZD, 100-AUDNZD, NZDCHF |
| USD | 100-AUDUSD, USDCHF, USDCAD, 100-EURUSD, 100-GBPUSD, USDJPY, 100-NZDUSD |

A estratégia armazena o último candle completado por par, mantém o percentual mais recente e atualiza as forças das moedas após cada atualização.

## Trading e alertas
1. Quando todas as oito moedas têm dados válidos, a estratégia registra um snapshot (da mais forte para a mais fraca).
2. Se o valor mais forte é **≥ BuyLevel** e o valor mais fraco é **≤ SellLevel**, uma sugestão de trading é gerada.
3. A estratégia tenta encontrar o par direto (moeda forte como base, moeda fraca como cotação). Se não existir, verifica a orientação inversa e finalmente recorre a pares envolvendo USD.
4. O par detectado e a direção são registrados. Se `EnableAutoTrading` for `true` e `OrderVolume` for positivo, a estratégia emite uma ordem de mercado na direção sugerida. Posições opostas são zeradas automaticamente aumentando o tamanho da ordem.

Os sinais são limitados lembrando o último par sugerido e o lado, evitando alertas duplicados até que o mercado saia da zona de limiar.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Universe` | Lista de objetos `Security` representando os pares de FX (28 principais recomendados). | Obrigatório |
| `CandleType` | Especificação de candle para os cálculos (Diário, Semanal, Mensal, etc.). | Candles diários |
| `BuyLevel` | Limiar acima do qual uma moeda é tratada como sobrecomprada. | 90 |
| `SellLevel` | Limiar abaixo do qual uma moeda é tratada como sobrevendida. | 10 |
| `EnableAutoTrading` | Habilita ou desabilita a colocação automática de ordens. | false |
| `OrderVolume` | Volume para enviar com ordens de mercado quando o auto trading está habilitado. | 1 |
| `SymbolPrefix` | Prefixo opcional usado pelo broker/exchange (ex., `m.`). | "" |
| `SymbolSuffix` | Sufixo opcional usado pelo broker/exchange (ex., `.FX`). | "" |

## Passos de configuração
1. **Configuração do universo.** Adicione os 28 principais cruzamentos de Forex ao universo da estratégia. Os códigos devem corresponder aos nomes canônicos dos pares (ex., `EURUSD`). Use `SymbolPrefix`/`SymbolSuffix` se seu broker adicionar decorações.
2. **Seleção de período.** Escolha o `CandleType` desejado. Candles diários, semanais e mensais reproduzem os modos originais do painel.
3. **Ajuste de limiar.** Ajuste `BuyLevel`/`SellLevel` para controlar quão extrema a força precisa ser antes de gerar um sinal.
4. **Auto trading (opcional).** Defina `EnableAutoTrading` como true e defina `OrderVolume`. Deixe o sinalizador como false para receber apenas logs informativos.

## Notas de migração
- A camada GUI completa do painel MQL original é intencionalmente omitida. Toda a saída está disponível através do log da estratégia.
- Alertas são emitidos como entradas `LogInfo`; notificações push/email/desktop não foram portadas.
- Cálculos automáticos de stop-loss/alvo da versão MQL não são suportados; os traders devem gerenciar o risco usando os módulos de proteção do StockSharp ou controles de risco externos.
- O helper de licenciamento baseado em DES incorporado no script MQL foi removido.

## Uso recomendado
- Implante a estratégia dentro de uma sessão de conector que fornece candles em tempo real e históricos para todos os pares relevantes.
- Combine com um widget de gráfico para visualizar o par sugerido e monitorar as séries de candles subjacentes.
- Use os parâmetros `StartProtection` do StockSharp ou estratégias de risco separadas para impor stops/alvos globais.

## Considerações de teste
- Verifique que sua fonte de dados entrega candles completados para o período selecionado; a estratégia ignora barras não terminadas.
- Se alguns pares estiverem faltando no universo, a moeda correspondente não pode ser calculada e nenhum sinal será produzido.
- Ao avaliar o desempenho histórico, certifique-se de que o universo permaneça estático durante todo o backtest para evitar lacunas de força.
