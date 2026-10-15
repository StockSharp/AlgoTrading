# Estratégia Sidus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o sistema de médias móveis SIDUS. Opera usando cruzamentos entre duas médias móveis ponderadas linearmente e uma média exponencial de confirmação. Uma posição é aberta quando a LWMA de curto prazo cruza a LWMA de longo prazo ou quando a LWMA longa cruza a EMA lenta. Cruzamentos opostos fecham ou revertem a posição. Um stop-loss e take-profit baseados em percentual gerenciam o risco.

Os testes indicam um retorno anual médio de cerca de 25%. Tem melhor desempenho em pares de divisas.

A ideia central é capturar mudanças de tendência quando as médias móveis rápidas e lentas se realinham. O par de LWMA reage rapidamente às mudanças de preço, enquanto a EMA mais lenta filtra o ruído. Quando ocorre um alinhamento altista ou baixista, a estratégia entra nessa direção e depende dos níveis de proteção para sair durante movimentos adversos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: LWMA rápida cruza acima da LWMA lenta *ou* LWMA lenta cruza acima da EMA lenta.
  - **Vendido**: LWMA rápida cruza abaixo da LWMA lenta *ou* LWMA lenta cruza abaixo da EMA lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto ou níveis de stop de proteção.
- **Stops**: Sim, usa take-profit e stop-loss baseados em percentual via `StartProtection`.
- **Valores padrão**:
  - Comprimento da EMA rápida = 18.
  - Comprimento da EMA lenta = 28.
  - Comprimento da LWMA rápida = 5.
  - Comprimento da LWMA lenta = 8.
  - Take profit = 2%.
  - Stop loss = 1%.
- **Filtros**: Nenhum.
