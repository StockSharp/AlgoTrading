# Estratégia de Qualidade de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de exemplo que demonstra como operar usando as mudanças de direção de um preço mediano suavizado. O expert MQL original usava o indicador *Volatility Quality*; esta implementação o aproxima com uma média móvel simples do preço mediano.

## Lógica da estratégia
- Calcular o preço mediano de cada vela `(High + Low) / 2`.
- Suavizar o preço mediano com uma Média Móvel Simples (SMA).
- Determinar a cor do indicador: valores em ascensão são tratados como **cima** (cor 0) e valores em queda como **baixo** (cor 1).
- Quando a cor muda de cima para baixo, a estratégia fecha qualquer posição vendida e abre uma posição comprada.
- Quando a cor muda de baixo para cima, a estratégia fecha qualquer posição comprada e abre uma posição vendida.
- Gerenciamento de risco básico é aplicado via níveis fixos de stop loss e take profit.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `Length` | Período de suavização para a SMA aplicada ao preço mediano. |
| `Candle Type` | Período das velas utilizadas para os cálculos. |

## Aviso
Este exemplo é fornecido para fins educacionais. Ele simplifica o algoritmo original e pode se comportar de maneira diferente da versão MQL. Use por sua conta e risco.
