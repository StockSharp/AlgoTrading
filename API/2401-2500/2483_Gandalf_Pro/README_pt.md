# Estratégia Gandalf PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista Gandalf PRO do MQL. Ela calcula duas médias móveis (LWMA e SMA) sobre o fechamento
das velas e as insere na lógica de suavização recursiva original para projetar um preço-alvo futuro. Quando o alvo projetado se
afasta o suficiente do fechamento atual (15 passos de preço ou mais), a estratégia abre uma ordem de mercado e armazena os níveis
de stop-loss e take-profit derivados da previsão.

Operações compradas exigem que o alvo suavizado esteja acima do fechamento atual em pelo menos 15 passos; operações vendidas exigem
que o alvo esteja abaixo do fechamento pela mesma margem. Os stop-losses são definidos em passos de preço e convertidos usando o
passo de preço do instrumento. Os níveis de take-profit são iguais ao alvo projetado e são monitorados a cada vela concluída. Os
multiplicadores de risco redimensionam o volume base da estratégia, possibilitando regras simples de gestão de capital.

## Parâmetros
- Tipo de vela
- Ativar compra
- Comprimento de compra
- Fator de preço de compra
- Fator de tendência de compra
- Stop-loss de compra
- Multiplicador de risco de compra
- Ativar venda
- Comprimento de venda
- Fator de preço de venda
- Fator de tendência de venda
- Stop-loss de venda
- Multiplicador de risco de venda
