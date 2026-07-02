# Estratégia Et4 MTC v1 (StockSharp Conversão)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- **Origem**: MetaTrader 4 consultores especialistas `et4_MTC_v1.mq4` da coleção GlobeInvestFund.
- **Objetivo**: Fornecer um modelo nativo StockSharp que espelhe os auxiliares de gerenciamento de dinheiro e salvaguardas de tempo do consultor original, deixando a lógica de entrada/saída comercial aberta para desenvolvimento adicional.
- **Estilo de negociação**: Estratégia esqueleto – nenhuma entrada automática é gerada por padrão. A classe se concentra em impor restrições de tempo e replicar a interface de parâmetros do script MQL4 para que possa servir como base para regras personalizadas.

## Recursos principais
1. **Paridade de parâmetros**
   - Expõe propriedades `TakeProfit`, `StopLoss`, `Slippage`, `Lots` e `EnableLogging` que mapeiam um a um com as variáveis externas do especialista.
   - Adiciona `TradeCooldown` para descrever o atraso de 30 segundos codificado entre as operações no código-fonte.
   - Publica o contexto de dados do gráfico por meio de `CandleType` para emular o comportamento do "período atual" dos gráficos MetaTrader.
2. **Dimensionamento de posição baseado em equilíbrio**
   - Suporta entradas de lote negativo (padrão do script original) para derivar o volume do pedido do saldo da conta: `floor((balance / 1000 * |Lots|) / 10) / 10`, com um mínimo de 0,1 lote.
3. **Aplicação do período de espera comercial**
   - Bloqueia qualquer outra tentativa de negociação até que `TradeCooldown` decorra após a atividade de pedido mais recente (registro, modificação, cancelamento ou negociação preenchida). Isso reflete a proteção `CurTime() - LastTradeTime < 30` em `start()`.
4. **Detecção de Nova Vela**
   - Mantém a semântica `CheckLevels` marcando `IsNewCandle` por meio de uma comparação de tempo entre velas concluídas subsequentes. Embora o sinalizador seja interno, os ganchos em `OpenPosition`, `ManagePosition` e `ClosePosition` podem usá-lo quando a lógica personalizada é adicionada.
5. **Uso de StockSharp API de alto nível**
   - Utiliza `SubscribeCandles().Bind(...)` para entrega de dados.
   - Aplica-se `StartProtection()` uma vez na inicialização, seguindo as práticas recomendadas da estrutura.
   - Não aloca coleções personalizadas nem solicita explicitamente o histórico de indicadores, alinhando-se com as diretrizes de todo o projeto.

## Referência de parâmetro
| Propriedade | Padrão | Otimizável | Descrição |
| --- | --- | --- | --- |
| `TakeProfit` | 150 | ✔️ | Distância alvo em pontos (espaço reservado para regras de saída personalizadas). |
| `Lots` | -10 | ✔️ | Lotes fixos quando ≥ 0; dimensionamento proporcional ao equilíbrio quando negativo. |
| `StopLoss` | 50 | ✔️ | Distância de parada em pontos, pronta para lógica de extensão. |
| `Slippage` | 3 | ✖️ | Tolerância de execução em pontos; preservados para compatibilidade. |
| `EnableLogging` | `false` | ✖️ | Imprime mensagens informativas quando o cooldown bloqueia negociações. |
| `TradeCooldown` | 30 segundos | ✖️ | Atraso mínimo entre negociações consecutivas. |
| `CandleType` | Velas com intervalo de tempo de 1 minuto | ✖️ | Assinatura de dados de mercado usada para timing de velas. |

## Fluxo de Execução
1. **Inicialização**
   - Calcula o `Volume` inicial usando o auxiliar de dimensionamento com reconhecimento de equilíbrio.
   - Assina o fluxo de velas configurado e inicia mecanismos de proteção.
2. **Na vela fechada**
   - Confirma que a vela terminou antes de prosseguir (equivalente ao fechamento de `Time[0]` no MT4).
   - Atualiza o rastreador new-candle (`_isNewCandle`).
   - Verifica `IsFormedAndOnlineAndAllowTrading()` para respeitar o estado do motor.
   - Aborta se o resfriamento da negociação estiver ativo, registrando o próximo horário disponível quando ativado.
   - Executa ganchos de espaço reservado (`OpenPosition`, `ManagePosition`, `ClosePosition`), retornando antecipadamente quando qualquer etapa executa uma ação.
3. **Retornos de chamada para pedidos e negociações**
   - `OnOrderRegistered`, `OnOrderChanged`, `OnOrderCanceled` e `OnNewMyTrade` atualizam `_lastTradeTime`, garantindo que cada tipo de operação redefina o resfriamento assim como as funções do wrapper (`MOrderSend`, `MOrderModify`, etc.) fizeram no código original.

## Estendendo o modelo
- Implemente a lógica de entrada dentro de `OpenPosition` (retorne `true` após enviar pedidos para interromper o processamento adicional na mesma vela).
- Insira o comportamento de gerenciamento de parada dentro de `ManagePosition` usando os parâmetros preservados.
- Preencha `ClosePosition` com regras de saída. O método atualmente retorna `false` para corresponder ao comportamento inativo do script de origem.
- Use `_isNewCandle` se as regras precisarem ser acionadas uma vez por barra.

## Portando Notas
- O especialista MQL4 foi enviado sem regras de negociação; apenas rotinas de infraestrutura estavam presentes. Consequentemente, a conversão StockSharp prioriza a paridade de recursos de suporte em vez de adicionar indicadores especulativos.
- Todos os comentários são escritos em inglês, obedecendo aos padrões do repositório.
- Tabulações são usadas para recuo para corresponder às diretrizes de estilo definidas em `AGENTS.md`.
- A tradução do Python é omitida intencionalmente, de acordo com a solicitação de conversão.

## Etapas de uso
1. Faça referência a `Et4MtcV1Strategy` em um projeto StockSharp e atribua um `Security` e um `Portfolio` antes de começar.
2. Ajuste `Lots` ou outros parâmetros por meio das propriedades fornecidas ou vinculações de IU.
3. Substitua os métodos de espaço reservado ou herde da classe para injetar lógica de negociação concreta.
4. Execute a estratégia; a proteção de resfriamento garante que não haja operações consecutivas dentro do intervalo especificado.

## Teste
- Nenhum teste automatizado acompanha este modelo porque a fonte upstream também não possuía regras executáveis. As extensões manuais da estratégia devem introduzir testes relevantes quando o comportamento comercial concreto for implementado.
