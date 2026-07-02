# Estratégia de grade de stops pendentes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia de grade de stops pendentes** é uma conversão direta do expert advisor do MetaTrader 4 `new.mq4`. A estratégia mantém duas escadas simétricas de ordens pendentes:

- Uma sequência de ordens buy stop acima do preço ask atual.
- Uma sequência de ordens sell stop abaixo do preço bid atual.

Cada nível adicional aumenta tanto a distância da ordem quanto o volume negociado proporcionalmente à sua posição dentro da escada. Alvos de stop-loss e take-profit são atribuídos individualmente a cada ordem.

## Lógica de negociação
1. A estratégia assina dados Level 1 e acompanha continuamente os últimos melhores preços bid e ask.
2. Assim que dados de mercado e permissões de negociação estão disponíveis, ela calcula o tamanho de pip usando o passo de preço do ativo (com símbolos de cinco e três dígitos automaticamente normalizados para valores pip padrão).
3. Antes de colocar ordens, a estratégia valida se o volume base configurado respeita as restrições de volume mínimo e máximo do instrumento.
4. Para cada índice `i` de 1 até `NumberOfTrades`:
   - O volume da ordem é calculado como `BaseVolume * i` e arredondado para o passo permitido mais próximo.
   - Um buy stop é colocado em `Ask + DistancePips * i * pipSize` com offsets opcionais de stop-loss e take-profit.
   - Um sell stop é colocado em `Bid - DistancePips * i * pipSize` com offsets espelhados de stop-loss e take-profit.
5. Se qualquer ordem for executada, cancelada ou rejeitada, o espaço correspondente na escada é limpo e imediatamente reposto com uma nova ordem pendente quando os dados de mercado permitem.
6. `StartProtection()` incorporado é chamado na inicialização para ativar os controles de risco da plataforma.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BaseVolume` | Volume da primeira ordem pendente. Cada ordem subsequente multiplica essa base por seu índice. | `0.1` |
| `NumberOfTrades` | Número de ordens buy stop e sell stop mantidas simultaneamente. | `10` |
| `DistancePips` | Distância (em pips) entre o preço de mercado e cada nível de ordem pendente. | `10` |
| `StopLossPips` | Distância de stop-loss atribuída a cada ordem. Defina como zero para desabilitar a colocação de stop-loss. | `10` |
| `TakeProfitPips` | Distância de take-profit atribuída a cada ordem. Defina como zero para desabilitar a colocação de take-profit. | `10` |

Todos os parâmetros são expostos como parâmetros de estratégia otimizáveis e são validados para evitar valores negativos ou zero (quando aplicável).

## Observações adicionais
- Volumes são arredondados para o passo permitido mais próximo e limitados dentro dos limites mínimo e máximo definidos pela bolsa.
- Preços são normalizados com `Security.ShrinkPrice` para respeitar o tamanho do tick do instrumento.
- A estratégia não mantém estado histórico: ela reconstrói toda a escada sempre que o ativo é reiniciado ou permissões de negociação mudam.
- A lógica evita buffers manuais de indicadores em favor das vinculações da API de alto nível do StockSharp, seguindo as diretrizes de conversão de todo o projeto.
