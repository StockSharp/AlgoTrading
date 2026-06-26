# Estratégia de Back Kick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Back Kick** é um sistema de rompimento com hedge convertido do consultor especialista MetaTrader 5 `Back kick.mq5`. Mantém continuamente uma exposição de dois lados abrindo tanto uma posição comprada quanto uma vendida ao fechar de cada barra. Cada perna é protegida com distâncias simétricas de stop-loss e take-profit expressas em pips. O port StockSharp mantém as posições emparelhadas independentes rastreando seu estado manualmente em vez de depender da posição líquida agregada.

## Lógica de trading

1. Subscrever às velas do período configurado. Quando uma vela fecha e não há pernas com hedge ativas, solicitar um novo par de entradas.
2. Enviar imediatamente uma ordem de compra e uma de venda de mercado usando o mesmo volume. Cada perna mantém seus próprios deslocamentos de stop-loss e take-profit convertidos de distâncias em pips.
3. Monitorar os melhores preços de oferta/demanda a partir de dados de Nível 1. Se uma perna atingir seu preço de proteção, ela é fechada com uma ordem de mercado, enquanto a perna oposta permanece ativa até que sua própria saída seja acionada.
4. Após ambas as pernas estarem flat, a estratégia aguarda a próxima vela concluída antes de recriar o hedge.

Este comportamento espelha o especialista original, que reentra constantemente em ambas as direções para capturar "impulsos" abruptos de preço.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| ---- | ----------- | ------- | ----- |
| `OrderVolume` | Volume usado para cada perna de hedge. | `0.1` | Normalizado para o `VolumeStep` do instrumento, deve respeitar `MinVolume`/`MaxVolume`. |
| `StopLossPips` | Distância do stop-loss em pips. | `50` | Definir como `0` para desabilitar o stop de proteção para ambas as pernas. |
| `TakeProfitPips` | Distância do take-profit em pips. | `140` | Definir como `0` para desabilitar o take-profit de proteção. |
| `CandleType` | Período que aciona novos pares com hedge. | `15m` | Aceita qualquer `TimeFrame` suportado pelo ativo selecionado. |
| `LogDiagnostics` | Habilita log detalhado sobre entradas e saídas. | `false` | Útil para depurar sequências de preenchimento. |

## Notas de implementação

- **Conversão de pip** – O EA original ajusta o tamanho do pip para símbolos de 3/5 dígitos. O port StockSharp replica isso multiplicando o passo de preço por `10` quando necessário.
- **Modelo de hedge manual** – StockSharp usa posições líquidas, por isso a estratégia mantém o estado por perna (`PositionState`) e despacha ordens de mercado explícitas para saídas. Isso permite que o comportamento se assemelhe ao modo de conta com hedge do MT5.
- **Gestão de risco** – Níveis de stop-loss e take-profit são opcionais. Se qualquer um estiver desabilitado, essa perna só fechará quando o nível de proteção oposto for atingido ou via gerenciamento externo.
- **Serviço de proteção** – `StartProtection()` ainda é invocado para que o framework monitore desconexões inesperadas, mesmo que lógica de saída personalizada esteja implementada.

## Uso

1. Anexar a estratégia a um ativo com dados confiáveis de Nível 1 (oferta/demanda) e as velas do período desejado.
2. Configurar as distâncias de pip e o volume de negociação de acordo com seu perfil de risco.
3. Iniciar a estratégia; ela aguardará o próximo fechamento de vela antes de enviar o par com hedge.
4. Monitorar os logs ou o gráfico para observar como cada perna sai independentemente.

## Diferenças da versão MT5

- O gerenciamento de dinheiro baseado em percentual de risco não é transferido; use `OrderVolume` para controlar o tamanho da negociação.
- Como o StockSharp agrega posições de portfólio, a estratégia emula hedging através de contabilidade interna. Isso garante um comportamento próximo ao especialista original, mantendo compatibilidade com corretoras que netam posições.
- Verificações de nível de congelamento/stop específicas do corretor são omitidas. Em vez disso, a rotina de normalização de volume lança exceções descritivas se os limites da bolsa forem violados.

## Arquivos

- `CS/BackKickStrategy.cs` – Implementação da estratégia usando a API de alto nível do StockSharp.
- `README.md` – Documentação em inglês (este arquivo).
- `README_ru.md` – Documentação em russo.
- `README_zh.md` – Documentação em chinês.
