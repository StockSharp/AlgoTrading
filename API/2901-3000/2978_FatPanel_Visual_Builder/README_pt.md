# Estratégia FatPanel Construtor Visual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia FatPanel Construtor Visual** é uma tradução StockSharp do Consultor Especialista FAT Panel legado do MetaTrader. A implementação MQL original expunha uma tela de arrastar e soltar onde os usuários podiam vincular blocos de indicadores, lógica, estado e ordens. Este port em C# mantém a filosofia modular mas expressa cada conexão de bloco através de um único documento JSON que a estratégia lê ao iniciar.

## Como a conversão funciona

* O painel MQL criava botões, abas e um despachador baseado em temporizador. Essas preocupações de UI são removidas inteiramente. Em vez disso, a estratégia analisa o parâmetro `Configuration` (uma string JSON) e instancia os blocos de sinal e lógica correspondentes internamente.
* Os blocos são avaliados em cada vela finalizada do `CandleType` configurado. Os blocos de indicadores usam indicadores do StockSharp (`SMA`, `EMA`, `SMMA`, `WMA`) e nunca dependem de buffers manuais.
* Os blocos de ordens originais permitiam seleção de símbolo, stop-loss e take-profit em "pontos". No StockSharp, a segurança padrão é obtida de `Strategy.Security`; stop-loss e take-profit são reintroduzidos através dos parâmetros de estratégia `StopLossPoints` e `TakeProfitPoints` e são convertidos para distâncias de preço absolutas usando `Security.PriceStep`.
* Os filtros de estado de tempo e dia da semana espelham a lógica MQL. O sinal de preço de oferta subscreve dados Level1 apenas se pelo menos uma regra solicitar, replicando o comportamento de atualização sob demanda do despachador do painel.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Tipo de dados e período que alimenta cada sinal. |
| `Configuration` | Documento JSON descrevendo regras, condições e ações. O valor padrão reproduz a estratégia de cruzamento EMA/SMA de amostra do painel. |
| `Volume` | Tamanho de ordem padrão usado pelas ações a menos que uma regra o substitua. |
| `StopLossPoints` | Distância em passos de preço para a proteção de risco incorporada. Definir como `0` para desabilitar o stop-loss. |
| `TakeProfitPoints` | Distância em passos de preço para o take-profit incorporado. Definir como `0` para desabilitar. |

`StopLossPoints` e `TakeProfitPoints` só são ativados quando um valor positivo é fornecido **e** a segurança expõe um `PriceStep` válido.

## Estrutura de configuração

O esquema JSON é projetado para ficar próximo da linguagem de blocos do FAT Panel:

```json
{
  "rules": [
    {
      "name": "Nome da regra (opcional)",
      "all": [ /* condições que devem ser todas verdadeiras */ ],
      "any": [ /* condições opcionais, pelo menos uma deve ser verdadeira */ ],
      "none": [ /* condições opcionais que devem ser todas falsas */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

Cada item de condição tem um campo `type` com um dos seguintes valores:

| Tipo | Campos JSON | Propósito |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | Conecta dois blocos de sinal através de operadores lógicos (`Greater`, `Less`, `Equal`, `CrossAbove`, `CrossBelow`). Os limiares são interpretados como diferenças de preço absolutas. Os operadores de cruzamento disparam quando a vela anterior estava no lado oposto e a diferença atual excede o limiar. |
| `position` | `required` | Espelha os blocos de estado do painel FAT (`Any`, `FlatOnly`, `FlatOrShort`, `FlatOrLong`, `LongOnly`, `ShortOnly`). |
| `time` | `start`, `end` | Filtro de sessão intradiária no formato `HH:mm`. Início > fim mantém o comportamento noturno do painel MQL. |
| `dayOfWeek` | `days` | Lista de nomes de dias. Quando omitida, a condição assume de segunda a sexta por padrão, correspondendo aos padrões do painel. |

Os sinais (`left` / `right`) são definidos como:

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` suporta métodos `Simple`, `Exponential`, `Smoothed` e `LinearWeighted` com qualquer das fontes de preço OHLC. O indicador compartilha o fluxo de velas da estratégia, assim como o painel usava períodos selecionados no gráfico.
* `Bid` usa o último melhor preço de oferta das atualizações de level1 (recai para o fechamento da vela até que uma cotação chegue).
* `Constant` reproduz o bloco HLINE e produz um nível estático.

As ações de regras replicam os blocos de ordens:

* `Buy` – abre ou reverte para uma posição comprada quando a posição atual está zerada ou vendida.
* `SellShort` – abre ou reverte para uma posição vendida quando a posição está zerada ou comprada.
* `Close` – sai de qualquer posição aberta usando `ClosePosition()`.

Um `volume` por ação pode substituir o parâmetro `Volume` padrão.

## Fluxo de execução

1. Quando a estratégia inicia, ela analisa o JSON de configuração. Documentos inválidos param a estratégia e emitem um log de erro.
2. Os indicadores são instanciados e armazenados em cache para que múltiplas regras possam reutilizar as mesmas definições de sinal sem cálculos duplicados.
3. Para cada vela finalizada a estratégia atualiza os valores de sinal e depois avalia cada regra em ordem. As condições `all` devem todas passar, `any` deve passar pelo menos uma vez (se fornecido), e `none` deve falhar completamente.
4. Se uma ação for acionada, a estratégia registra o nome da regra e executa a ordem a mercado solicitada.
5. As proteções opcionais de stop-loss e take-profit são armadas uma vez durante `OnStarted` usando as distâncias em pontos fornecidas.

## Limitações e notas

* Apenas a `Strategy.Security` principal é suportada. O roteamento entre símbolos do painel original exigiria múltiplas instâncias de estratégia.
* O despachador MQL permitia aninhamento profundo de blocos de lógica (por exemplo, AND dentro de OR). A estrutura JSON fornece controle similar através dos arrays `all`/`any`/`none`, mas grafos extremamente complexos ainda podem precisar de adaptação manual.
* O operador `Cross` usa apenas o último ローソク足. O bloco MQL expunha um buffer de retrocesso e delta em "pontos"; adapte o campo `threshold` para emular a sensibilidade necessária.
* Recursos de UI como posições de arrastar, janelas de diálogo e ícones de barra de ferramentas não têm equivalente direto no StockSharp e são intencionalmente omitidos.

## Configuração de amostra

A configuração padrão incorporada na estratégia é reproduzida abaixo para conveniência:

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

Esta amostra espelha o modelo de painel de ações: abrir uma posição comprada em um cruzamento de alta EMA 20/50 SMA durante a sessão regular e fechar a posição no cruzamento inverso.
