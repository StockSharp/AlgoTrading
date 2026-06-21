# Estratégia Exp HLRSign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa a lógica do indicador HLRSign no StockSharp.
Abre e fecha posições quando a Relação Alto-Baixo (HLR) cruza níveis predefinidos.

## Como Funciona

- Calcula os valores do Canal Donchian sobre um intervalo configurável.
- Calcula o valor HLR como a posição percentual do preço médio dentro do canal.
- Gera sinais de compra ou venda quando o HLR cruza os limites superior ou inferior dependendo do modo selecionado:
  - **ModeIn** – comprar ao cruzar acima do nível superior e vender ao cruzar abaixo do nível inferior.
  - **ModeOut** – vender ao cruzar abaixo do nível superior e comprar ao cruzar acima do nível inferior.
- Permite habilitar ou desabilitar a abertura e o fechamento de posições compradas e vendidas separadamente.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Mode` | Modo de operação do indicador (ModeIn ou ModeOut). |
| `Range` | Período de retrocesso para preços máximos e mínimos. |
| `UpLevel` | Limite superior em percentual para o HLR. |
| `DnLevel` | Limite inferior em percentual para o HLR. |
| `CandleType` | Período das velas usadas para cálculos. |
| `BuyOpen` | Permitir abertura de posições compradas. |
| `SellOpen` | Permitir abertura de posições vendidas. |
| `BuyClose` | Permitir fechamento de posições compradas. |
| `SellClose` | Permitir fechamento de posições vendidas. |

## Notas

- A estratégia usa a API de alto nível com o indicador `DonchianChannels`.
- Processa apenas velas fechadas e verifica permissões de posição antes de negociar.
- Nenhum nível de stop-loss ou take-profit está definido; a proteção de posição pode ser adicionada manualmente.
